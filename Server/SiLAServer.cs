using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Server.Binary;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Base class for all SiLA 2 Servers.
    /// </summary>
    [Export(typeof(ISiLAServer))]
    [Export(typeof(ISiLAService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SiLAServer : ISiLAService, ISiLAServer
    {
        #region Members

        private readonly ILoggingChannel _logger;

        /// <summary>The gRPC server.</summary>
        public Grpc.Core.Server GrpcServer { get; }

        /// <summary>The list of implmented features.</summary>
        private readonly List<IFeatureProvider> _implementedFeatures;

        /// <summary>Discovery listener</summary>
        private readonly ServiceAnnouncer _announcer;

        /// <summary>All current observable command executions, with the identifying CommandExecutionUUID, the command identifier and the current execution state.</summary>
        private readonly List<ObservableCommandExecution> _commandExecutions = new List<ObservableCommandExecution>();

        private readonly IEnumerable<IRequestInterceptor> _interceptors;
        private readonly Dictionary<string, List<IRequestInterceptor>> _cachedInterceptorList = new Dictionary<string, List<IRequestInterceptor>>();

        private readonly IFileManagement _fileManagement;
        private readonly BinaryUpload _upload;
        private readonly BinaryDownload _download;
        private readonly LoggingInterceptor _loggingInterceptor;

        private readonly Timer _cleanupTimer;

        #endregion

        #region Utils

        /// <summary>
        /// Check if the specified TCP port is available
        /// If the port is already in use, an SocketException is thrown
        /// </summary>
        private static void EnsurePortIsAvailable(int port)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Parse("0.0.0.0"), port);
            tcpListener.Start();
            tcpListener.Stop();
        }

        private IEnumerable<IRequestInterceptor> GetInterceptors(string commandIdentifier)
        {
            if (!_cachedInterceptorList.TryGetValue(commandIdentifier, out var interceptors))
            {
                var feature = _implementedFeatures.FirstOrDefault(f => commandIdentifier.StartsWith(f.FeatureDefinition.FullyQualifiedIdentifier))?.FeatureDefinition;
                interceptors = _interceptors.Where(i => i.IsInterceptRequired(feature, commandIdentifier)).OrderByDescending(r => r.Priority).ToList();

                _cachedInterceptorList.Add(commandIdentifier, interceptors);
            }

            return interceptors;
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public string ServerName { get; private set; }

        /// <inheritdoc />
        public string ServerDescription { get; }

        /// <inheritdoc />
        public string ServerVendorURL { get; }

        /// <inheritdoc />
        public string ServerVersion { get; }

        /// <inheritdoc />
        public string ServerType { get; }

        /// <inheritdoc />
        public string ServerUUID { get; }

        /// <inheritdoc />
        public void SetServerName(string serverName)
        {
            ServerName = serverName;
        }

        #endregion

        #region Constructors and destructors

        /// <summary>
        /// Creates a new instance of the SiLA2Server class, adds the SiLAService Feature implementation, creates a gRPC Server and registers the server for Server discovery.
        /// </summary>
        /// <param name="startupInfo">The information the server requires to start up</param>
        /// <param name="loggingChannelFactory">The factory for logging</param>
        /// <param name="interceptors">Interceptors to intercept any commands issued to the server</param>
        /// <param name="fileManagement">The file management used by this server</param>
        [ImportingConstructor]
        public SiLAServer(ServerStartInformation startupInfo, ILoggingChannelFactory loggingChannelFactory, [ImportMany] IEnumerable<IRequestInterceptor> interceptors, IFileManagement fileManagement)
        {
            _logger = loggingChannelFactory.CreateChannel(typeof(SiLAServer));
            _interceptors = interceptors;
            _upload = new BinaryUpload(fileManagement, this);
            _download = new BinaryDownload(fileManagement);
            _fileManagement = fileManagement;
            EnsurePortIsAvailable(startupInfo.Port);

            ServerType = startupInfo.Info.Type;
            ServerDescription = startupInfo.Info.Description;
            ServerVendorURL = startupInfo.Info.VendorURI;
            ServerVersion = startupInfo.Info.Version;
            ServerUUID = startupInfo.ServerUuid.ToString();
            ServerName = startupInfo.Name ?? ServerType;
            _implementedFeatures = new List<IFeatureProvider>();
            _loggingInterceptor = new LoggingInterceptor( _logger );

            GrpcServer = new Grpc.Core.Server();

            ServerCredentials credentials;
            if (File.Exists("server.crt") && File.Exists("ca.crt") && File.Exists("server.key"))
            {
                var cacert = File.ReadAllText(@"ca.crt");
                var servercert = File.ReadAllText(@"server.crt");
                var serverkey = File.ReadAllText(@"server.key");
                var keypair = new KeyCertificatePair(servercert, serverkey);
                _logger.Log("Found certificates, starting server with TLS.".ToString, Severity.Info);
                credentials = new SslServerCredentials(new List<KeyCertificatePair>() { keypair }, cacert, SslClientCertificateRequestType.DontRequest);
            }
            else
            {
                _logger.Log($"No certificates found in folder {Environment.CurrentDirectory}. Server will start with plain-text communication".ToString, Severity.Warning);
                credentials = ServerCredentials.Insecure;
            }

            // create gRPC server such that it uses only the specified network interface
            if (startupInfo.NetworkInterface != null)
            {
                var unicastAddresses = startupInfo.NetworkInterface.GetIPProperties().UnicastAddresses.Select(u => u.Address);
                _logger.Log("Running on the following addresses:".ToString, Severity.Info);
                foreach (var v4addr in unicastAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork))
                {
                    _logger.Log(v4addr.ToString, Severity.Info);
                    GrpcServer.Ports.Add(new ServerPort(v4addr.ToString(), startupInfo.Port, credentials));
                }
                _announcer = new ServiceAnnouncer(ServerUUID, (ushort)startupInfo.Port, startupInfo.NetworkInterface, loggingChannelFactory);
            }
            else
            {
                _logger.Log("Server is running without Discovery!".ToString, Severity.Warning);
                // if no interface was specified, the SiLA server will (try to) run on all of them
                GrpcServer.Ports.Add(new ServerPort("0.0.0.0", startupInfo.Port, credentials));
            }

            GrpcServer.Services.Add(_upload.CreateServiceDefinition());
            GrpcServer.Services.Add(_download.CreateServiceDefinition());

            var cleanupInterval = TimeSpan.FromSeconds( 30 );
            _cleanupTimer = new Timer( Cleanup, null, cleanupInterval, cleanupInterval );
        }

        private void Cleanup( object state )
        {
            lock( _commandExecutions )
            {
                for( int i = _commandExecutions.Count - 1; i >= 0; i-- )
                {
                    var commandExecution = _commandExecutions[i];
                    var responseTask = commandExecution.Command.Response;
                    if( responseTask.IsCanceled || responseTask.IsCompleted || responseTask.IsFaulted )
                    {
                        if( !commandExecution.Expiration.HasValue )
                        {
                            commandExecution.Expiration = DateTime.Now + TimeSpan.FromMinutes( 5 );
                        }
                        else if (commandExecution.Expiration.Value < DateTime.Now)
                        {
                            _logger.Log($"Forgetting command execution {commandExecution.CommandExecutionID} ({commandExecution.CommandIdentifier}).".ToString);
                            _commandExecutions.RemoveAt(i);
                            
                        }
                    }
                }
            }
        }

        #endregion

        #region gRPC server

        /// <summary>
        /// Starts the server
        /// </summary>
        public void StartServer()
        {
            _logger.Log("Starting server".ToString, Severity.Info);
            GrpcServer.Start();
            if (_announcer != null)
            {
                _announcer.Start();
                _logger.Log($"Announcing as '{_announcer.Profile.FullyQualifiedName}'".ToString, Severity.Info);
            }
        }

        /// <summary>
        /// Shuts down the server
        /// </summary>
        /// <returns>An awaitable task that shuts down the server</returns>
        public async Task ShutdownServer()
        {
            lock( _commandExecutions )
            {
                foreach( var execution in _commandExecutions )
                {
                    if( execution.Command.IsCancellationSupported )
                    {
                        execution.Command.Cancel();
                    }
                }
            }
            _announcer?.Dispose();
            _cleanupTimer.Dispose();
            _logger.Log("Shutting down Grpc server".ToString, Severity.Info);
            await GrpcServer.ShutdownAsync();
        }

        #endregion

        #region Feature discovery

        /// <summary>
        /// Adds the given feature to the server
        /// </summary>
        /// <param name="feature">The feature to expose</param>
        public void AddFeature(IFeatureProvider feature)
        {
            _implementedFeatures.Add(feature);
            GrpcServer.Services.Add( feature.CreateServiceDefinition().Intercept(_loggingInterceptor) );
            _logger.Log( $"Exposing feature {feature.FeatureDefinition.FullyQualifiedIdentifier}".ToString, Severity.Info );
        }

        /// <summary>
        /// Gets the feature object with the given feature identifier.
        /// </summary>
        /// <param name="qualifiedFeatureIdentifier">The (full qualified) feature identifier.</param>
        /// <returns>The feature description.</returns>
        public string GetFeatureDefinition(string qualifiedFeatureIdentifier)
        {
            if (!ImplementedFeatures.Contains(qualifiedFeatureIdentifier))
            {
                throw new UnimplementedFeatureException($"Given Feature identifier: \"{qualifiedFeatureIdentifier}\"");
            }

            var feature = _implementedFeatures.First(f => f.FeatureDefinition.FullyQualifiedIdentifier == qualifiedFeatureIdentifier).FeatureDefinition;
            return FeatureSerializer.SaveToString(feature).Replace(Environment.NewLine, string.Empty);
        }

        /// <summary>
        /// Gets a list of fully qualified identifiers of the implemented features.
        /// </summary>
        public ICollection<string> ImplementedFeatures
        {
            get { return _implementedFeatures.Select(feature => feature.FeatureDefinition.FullyQualifiedIdentifier).ToList(); }
        }

        /// <summary>
        /// Gets the features implemented by the current server
        /// </summary>
        public IEnumerable<IFeatureProvider> Features => _implementedFeatures.AsReadOnly();

        /// <inheritdoc />
        public IObservableCommand[] FindCommandExecutions( Predicate<CommandState> stateFilter )
        {
            lock (_commandExecutions)
            {
                return _commandExecutions
                    .Where( ex => stateFilter(ex.Command.State.State))
                    .Select( ex => ex.Command )
                    .ToArray();
            }
        }

        #endregion

        #region Observable Commands

        /// <summary>
        /// Invokes the given (non-observable) command including the complete request pipeline
        /// </summary>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <typeparam name="TResponse">The type of the response</typeparam>
        /// <param name="command">The actual command</param>
        /// <param name="request">The request object</param>
        /// <param name="context">The call context in which the command is executed</param>
        /// <returns>An awaitable task with the command response</returns>
        public Task<TResponse> InvokeCommand<TRequest, TResponse>(Func<TRequest, TResponse> command, TRequest request,
            ServerCallContext context)
            where TRequest : ISilaRequestObject
        {
            return Task.Factory.StartNew( () => InvokeCommandCore( command, request, context ) );
        }

        private TResponse InvokeCommandCore<TRequest, TResponse>( Func<TRequest, TResponse> command, TRequest request,
            ServerCallContext context )
            where TRequest : ISilaRequestObject
        {
            using( new ActivityBlock( "Request of " + request.CommandIdentifier ) )
            {
                var interceptions = RequestInterceptions( request.CommandIdentifier, context );
                try
                {
                    var result = command( request );
                    if( interceptions != null )
                    {
                        foreach( var interception in interceptions )
                        {
                            interception.ReportSuccess();
                        }
                    }

                    return result;
                }
                catch( Exception exception )
                {
                    if( interceptions != null )
                    {
                        foreach( var interception in interceptions )
                        {
                            interception.ReportException( exception );
                        }
                    }

                    if( exception is OperationCanceledException )
                    {
                        throw ErrorHandling.CreateCancellationError( exception.Message );
                    }
                    else if( !(exception is RpcException) )
                    {
                        throw ErrorHandling.CreateUndefinedExecutionError( exception );
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Invokes the given (non-observable) command including the complete request pipeline
        /// </summary>
        /// <typeparam name="TResponse">The type of the response</typeparam>
        /// <param name="command">The actual command</param>
        /// <param name="commandIdentifier">The identifier of the command</param>
        /// <param name="context">The call context in which the command is executed</param>
        /// <returns>An awaitable task with the command response</returns>
        public async Task<TResponse> InvokeCommand<TResponse>(Func<Task<TResponse>> command, string commandIdentifier, ServerCallContext context)
        {
            using( new ActivityBlock( "Request of " + commandIdentifier ) )
            {
                var interceptions = RequestInterceptions( commandIdentifier, context );
                try
                {
                    var result = await command();
                    if( interceptions != null )
                    {
                        foreach( var interception in interceptions )
                        {
                            interception.ReportSuccess();
                        }
                    }

                    return result;
                }
                catch( Exception exception )
                {
                    if( interceptions != null )
                    {
                        foreach( var interception in interceptions )
                        {
                            interception.ReportException( exception );
                        }
                    }

                    if( exception is OperationCanceledException )
                    {
                        throw ErrorHandling.CreateCancellationError( exception.Message );
                    }
                    else if( !(exception is RpcException) )
                    {
                        throw ErrorHandling.CreateUndefinedExecutionError( exception );
                    }

                    throw;
                }
            }
        }

        private Stack<IRequestInterception> RequestInterceptions(string commandIdentifier, ServerCallContext context)
        {
            Stack<IRequestInterception> interceptions = null;
            try
            {
                foreach( var interceptor in GetInterceptors( commandIdentifier ) )
                {
                    var interception = interceptor.Intercept( commandIdentifier, this, context );
                    if( interception != null )
                    {
                        if( interceptions == null ) interceptions = new Stack<IRequestInterception>();
                        interceptions.Push( interception );
                    }
                }
            }
            catch( Exception exception )
            {
                if( interceptions != null )
                {
                    while( interceptions.Count > 0 )
                    {
                        interceptions.Pop()?.ReportException(exception);
                    }
                }

                if(exception is OperationCanceledException)
                {
                    throw ErrorHandling.CreateCancellationError( exception.Message );
                }
                else if(!(exception is RpcException))
                {
                    throw ErrorHandling.CreateUndefinedExecutionError( exception );
                }

                throw;
            }

            return interceptions;
        }

        /// <inheritdoc />
        public string AddObservableCommandExecution(string commandIdentifier, IObservableCommand command, ServerCallContext context)
        {
            var interceptions = RequestInterceptions( commandIdentifier, context );
            // create command ID
            var commandExecutionId = Guid.NewGuid().ToString();

            var execution = new ObservableCommandExecution(commandExecutionId, commandIdentifier, command, interceptions);

            lock( _commandExecutions )
            {
                _commandExecutions.Add( execution );
            }
            if( !command.IsStarted )
            {
                command.Start();
            }

            return commandExecutionId;
        }

        /// <inheritdoc />
        public Task<T> CreatePropertyTask<T>(string propertyIdentifier, T value, ServerCallContext context)
        {
            return Task.FromResult(value);
        }

        /// <inheritdoc />
        public Stream ResolveStoredBinary(string identifier)
        {
            _upload.WaitUploadComplete( identifier );
            return _fileManagement.OpenRead(identifier);
        }

        /// <inheritdoc />
        public bool ShouldStoreBinary(long length)
        {
            return length > 200 * 1024;
        }

        /// <inheritdoc />
        public string StoreBinary(Stream data)
        {
            var identifier = _fileManagement.CreateNew(data.Length);
            using (var stream = _fileManagement.OpenWrite(identifier))
            {
                data.CopyTo(stream);
            }

            return identifier;
        }

        /// <inheritdoc />
        public string StoreBinary(FileInfo file)
        {
            return _fileManagement.CreateIdentifier(file);
        }

        /// <inheritdoc />
        public void Delete( string identifier )
        {
            _upload.DeleteUpload( identifier );
        }

        /// <inheritdoc />
        public FileInfo ResolveStoredBinaryFile( string identifier )
        {
            _upload.WaitUploadComplete( identifier );
            return _fileManagement.Locate( identifier );
        }

        /// <inheritdoc />
        public IObservableCommand GetCommandExecution( string commandExecutionId )
        {
            lock(_commandExecutions)
            {
                var command = _commandExecutions.Find( c => c.CommandExecutionID == commandExecutionId );
                if(command == null)
                {
                    // if command execution ID hasn't been found -> raise SiLA Framework Error
                    throw ErrorHandling.CreateFrameworkError( FrameworkErrorType.InvalidCommandExecutionUuid );
                }

                return command.Command;
            }
        }

        #endregion

    }
}

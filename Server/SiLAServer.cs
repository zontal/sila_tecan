using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Server.Binary;
using Tecan.Sila2.Server.ServiceDefinition;
using Tecan.Sila2.ServerPooling;

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

        private readonly ILog _logger = LogManager.GetLogger<SiLAServer>();
        private readonly IServerProvider _serverProvider;

        /// <summary>The list of implmented features.</summary>
        private readonly List<IFeatureProvider> _implementedFeatures;

        /// <summary>Discovery listener</summary>
        private readonly ServiceAnnouncer _announcer;

        /// <summary>All current observable command executions, with the identifying CommandExecutionUUID, the command identifier and the current execution state.</summary>
        private readonly List<ObservableCommandExecution> _commandExecutions = new List<ObservableCommandExecution>();

        private readonly IEnumerable<IRequestInterceptor> _interceptors;
        private readonly Dictionary<string, List<IRequestInterceptor>> _cachedInterceptorList = new Dictionary<string, List<IRequestInterceptor>>();

        private readonly IFileManagement _fileManagement;
        private readonly IConfigurationStore _configurationStore;
        private readonly BinaryUpload _upload;
        private readonly BinaryDownload _download;
        private readonly LoggingInterceptor _loggingInterceptor;

        private readonly Timer _cleanupTimer;

        private readonly ServerPoolDispatcher _poolDispatcher;

        internal const string ServerConfigurationName = "default";

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
            var configuration = _configurationStore.Read<ServerConfiguration>( "default" );
            if (configuration != null)
            {
                configuration.Name = serverName;
                try
                {
                    _configurationStore.Write( ServerConfigurationName, configuration );
                }
                catch(Exception exception)
                {
                    _logger.Error( "Could not save server configuration", exception );
                }
            }
        }

        #endregion

        #region Constructors and destructors

        private void TryPersistConfiguration( ServerStartInformation startupInfo, IConfigurationStore configurationStore, int port )
        {
            try
            {
                configurationStore.Write( ServerConfigurationName, new ServerConfiguration
                {
                    Identifier = ServerUUID,
                    InterfaceFilter = startupInfo.NetworkInterfaces,
                    Name = startupInfo.Name,
                    Port = port
                } );
            }
            catch(Exception exception)
            {
                _logger.Warn( $"Could not save server configuration: {exception.Message}" );
            }
        }

        /// <summary>
        /// Creates a new instance of the SiLA2Server class, adds the SiLAService Feature implementation, creates a gRPC Server and registers the server for Server discovery.
        /// </summary>
        /// <param name="startupInfo">The information the server requires to start up</param>
        /// <param name="configurationStore">An object to read and write server configuration</param>
        /// <param name="interceptors">Interceptors to intercept any commands issued to the server</param>
        /// <param name="fileManagement">The file management used by this server</param>
        /// <param name="serverProvider">The actual gRPC server the SiLA2 server should be running on</param>
        [ImportingConstructor]
        public SiLAServer(ServerStartInformation startupInfo, IConfigurationStore configurationStore, [ImportMany] IEnumerable<IRequestInterceptor> interceptors, IFileManagement fileManagement, IServerProvider serverProvider)
        {
            _interceptors = interceptors;
            _configurationStore = configurationStore;
            _upload = new BinaryUpload( fileManagement, this );
            _download = new BinaryDownload( fileManagement );
            _fileManagement = fileManagement;
            _poolDispatcher = new ServerPoolDispatcher( _upload, _download, this );
            _serverProvider = serverProvider;

            var configuration = configurationStore.Read<ServerConfiguration>( ServerConfigurationName );
            var port = (startupInfo.Port ?? configuration?.Port).GetValueOrDefault( 50052 );
            EnsurePortIsAvailable( port );

            var uuid = startupInfo.ServerUuid;
            if(uuid == Guid.Empty)
            {
                if(!(configuration?.Identifier != null && Guid.TryParse( configuration.Identifier, out uuid ) && uuid != Guid.Empty))
                {
                    uuid = Guid.NewGuid();
                }
            }

            _logger.Info( $"Starting server {uuid} on port {port}." );

            ServerType = startupInfo.Info.Type;
            ServerDescription = startupInfo.Info.Description;
            ServerVendorURL = startupInfo.Info.VendorUri;
            ServerVersion = startupInfo.Info.Version;
            ServerUUID = uuid.ToString();
            ServerName = startupInfo.Name ?? configuration?.Name ?? ServerType;
            _implementedFeatures = new List<IFeatureProvider>();
            _loggingInterceptor = new LoggingInterceptor(_logger);

            // create gRPC server such that it uses only the specified network interfaces
            if((startupInfo.NetworkInterfaces ?? configuration?.InterfaceFilter) != null)
            {
                var networking = new Networking();
                var filter = networking.CreateNetworkInterfaceFilter( startupInfo.NetworkInterfaces ?? configuration?.InterfaceFilter );
                
                _announcer = new ServiceAnnouncer( ServerUUID, (ushort)port, filter );

                _serverProvider.ConfigureForServer( uuid, port, filter );

                if(_serverProvider.Credentials is SslServerCredentials secureCredentials)
                {
                    var lines = secureCredentials.RootCertificates.Split( new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
                    for(int i = 0; i < lines.Length; i++)
                    {
                        _announcer.SetProperty( "ca" + i, lines[i] );
                    }
                }
                else
                {
                    _announcer.SetProperty( "encrypted", "false" );
                }
            }
            else
            {
                _logger.Warn( "Server is running without Discovery!" );
                // if no interface was specified, the SiLA server will (try to) run on all of them
                _serverProvider.ConfigureForServer( uuid, port, null );
            }

            _serverProvider.AddService( _upload.CreateServiceDefinition() );
            _serverProvider.AddService( _download.CreateServiceDefinition() );

            var cleanupInterval = TimeSpan.FromSeconds( 30 );
            _cleanupTimer = new Timer( Cleanup, null, cleanupInterval, cleanupInterval );

            if(configuration == null)
            {
                TryPersistConfiguration( startupInfo, configurationStore, port );
            }
        }

        private void Cleanup( object state )
        {
            lock( _commandExecutions )
            {
                for( int i = _commandExecutions.Count - 1; i >= 0; i-- )
                {
                    var commandExecution = _commandExecutions[i];
                    var responseTask = commandExecution.Command?.Response;
                    if( responseTask != null && ( responseTask.IsCanceled || responseTask.IsCompleted || responseTask.IsFaulted ) )
                    {
                        if( !commandExecution.Expiration.HasValue )
                        {
                            commandExecution.Expiration = DateTime.Now + TimeSpan.FromMinutes( 5 );
                        }
                        else if (commandExecution.Expiration.Value < DateTime.Now)
                        {
                            _logger.Debug($"Forgetting command execution {commandExecution.CommandExecutionID} ({commandExecution.CommandIdentifier}).");
                            if (commandExecution.Command is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
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
            _logger.Info("Starting server");
            if (_announcer != null)
            {
                _announcer.Start();
                _logger.Info($"Announcing as '{_announcer.Profile.FullyQualifiedName}'");
            }
        }

        /// <summary>
        /// Shuts down the server
        /// </summary>
        /// <returns>An awaitable task that shuts down the server</returns>
        public Task ShutdownServer()
        {
            return Task.Factory.StartNew( () =>
             {
                 lock(_commandExecutions)
                 {
                     foreach(var execution in _commandExecutions)
                     {
                         if(execution.Command.IsCancellationSupported)
                         {
                             execution.Command.Cancel();
                         }
                     }
                 }
                 _announcer?.Dispose();
                 _cleanupTimer.Dispose();
             });
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
            var definitionBuilder = new ServiceDefinitionBuilder();
            var builder = new ServerBuilder( this, feature.FeatureDefinition, _poolDispatcher, definitionBuilder );
            feature.Register( builder );
            _serverProvider.AddService( definitionBuilder.Build().Intercept(_loggingInterceptor) );
            _logger.Info( $"Exposing feature {feature.FeatureDefinition.FullyQualifiedIdentifier}" );
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
        public IServerErrorHandling ErrorHandling { get; } = new ServerErrorHandling();

        /// <inheritdoc />
        public ObservableCommandExecution[] FindCommandExecutions( Predicate<CommandState> stateFilter )
        {
            if (stateFilter == null)
            {
                throw new ArgumentNullException( nameof( stateFilter ) );
            }

            lock (_commandExecutions)
            {
                return _commandExecutions
                    .Where( ex => stateFilter(ex.Command.State.State))
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
        /// <param name="metadataRepository">The metadata entries to consider for the command</param>
        /// <returns>An awaitable task with the command response</returns>
        public Task<TResponse> InvokeCommand<TRequest, TResponse>(Func<TRequest, TResponse> command, TRequest request,
            IMetadataRepository metadataRepository)
            where TRequest : ISilaRequestObject
        {
            return Task.Factory.StartNew( () => InvokeCommandCore( command, request, metadataRepository ) );
        }

        private TResponse InvokeCommandCore<TRequest, TResponse>( Func<TRequest, TResponse> command, TRequest request,
            IMetadataRepository metadataRepository )
            where TRequest : ISilaRequestObject
        {
            _logger.Info( $"Executing command {request.CommandIdentifier}" );
            var interceptions = RequestInterceptions( request.CommandIdentifier, metadataRepository );
            try
            {
                var result = command( request );
                if(interceptions != null)
                {
                    foreach(var interception in interceptions)
                    {
                        interception.ReportSuccess();
                    }
                }

                return result;
            }
            catch(Exception exception)
            {
                _logger.Error( $"Error while executing command {request.CommandIdentifier}", exception );
                if(interceptions != null)
                {
                    foreach(var interception in interceptions)
                    {
                        interception.ReportException( exception );
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
        }

        /// <summary>
        /// Invokes the given (non-observable) command including the complete request pipeline
        /// </summary>
        /// <typeparam name="TResponse">The type of the response</typeparam>
        /// <param name="command">The actual command</param>
        /// <param name="commandIdentifier">The identifier of the command</param>
        /// <param name="metadataRepository">The metadata entries to consider for the command</param>
        /// <returns>An awaitable task with the command response</returns>
        public async Task<TResponse> InvokeCommand<TResponse>( Func<Task<TResponse>> command, string commandIdentifier, IMetadataRepository metadataRepository )
        {
            var interceptions = RequestInterceptions( commandIdentifier, metadataRepository );
            _logger.Info( $"Executing command {commandIdentifier}" );
            try
            {
                var result = await command();
                if(interceptions != null)
                {
                    foreach(var interception in interceptions)
                    {
                        interception.ReportSuccess();
                    }
                }

                return result;
            }
            catch(Exception exception)
            {
                _logger.Error( $"Error while executing command {commandIdentifier}", exception );
                if(interceptions != null)
                {
                    foreach(var interception in interceptions)
                    {
                        interception.ReportException( exception );
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
        }

        /// <summary>
        /// Requests interceptors for a call with the given command identifier
        /// </summary>
        /// <param name="commandIdentifier">Unique identifier of a command or property</param>
        /// <param name="metadataRepository">A repository with metadata information</param>
        /// <returns>A stack of request interceptions</returns>
        public Stack<IRequestInterception> RequestInterceptions(string commandIdentifier, IMetadataRepository metadataRepository )
        {
            Stack<IRequestInterception> interceptions = null;
            try
            {
                foreach( var interceptor in GetInterceptors( commandIdentifier ) )
                {
                    var interception = interceptor.Intercept( commandIdentifier, this, metadataRepository );
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

        /// <summary>
        /// Registers an observable command execution
        /// </summary>
        /// <param name="commandIdentifier">The unique identifier of the command that shall be executed</param>
        /// <param name="command">The observable command</param>
        /// <param name="metadataRepository">The metadata entries to consider for the command</param>
        /// <returns></returns>
        public string AddObservableCommandExecution(string commandIdentifier, IObservableCommand command, IMetadataRepository metadataRepository)
        {
            var interceptions = RequestInterceptions( commandIdentifier, metadataRepository );
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

        internal T GetPropertyValue<T>(string propertyIdentifier, Func<T> value, IMetadataRepository metadataRepository)
        {
            var interceptors = RequestInterceptions( propertyIdentifier, metadataRepository );
            try
            {
                _logger.Info( $"Obtaining value for property {propertyIdentifier}" );
                var result = value();
                if( interceptors != null )
                {
                    foreach( var interceptor in interceptors )
                    {
                        interceptor.ReportSuccess();
                    }
                }

                return result;
            }
            catch( Exception exception )
            {
                _logger.Error( $"Exception happened obtaining the value for {propertyIdentifier}", exception );
                if( interceptors != null )
                {
                    foreach( var interceptor in interceptors )
                    {
                        interceptor.ReportException(exception);
                    }
                }

                throw;
            }
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
        public ObservableCommandExecution GetCommandExecution( string commandExecutionId )
        {
            lock(_commandExecutions)
            {
                var command = _commandExecutions.Find( c => c.CommandExecutionID == commandExecutionId );
                if(command == null)
                {
                    // if command execution ID hasn't been found -> raise SiLA Framework Error
                    throw ServerErrorHandling.CreateFrameworkError( FrameworkErrorType.InvalidCommandExecutionUuid );
                }

                return command;
            }
        }

        /// <inheritdoc />
        public IDisposable ConnectToPool( string poolAddress )
        {
            _logger.Info( $"Connecting to server pool {poolAddress}" );
            var channel = new Channel( poolAddress, ChannelCredentials.Insecure );
            var connection = new PoolConnection( _poolDispatcher, channel.CreateCallInvoker().AsyncDuplexStreamingCall( PoolingConstants.ConnectSilaServerMethod, null, new CallOptions() ) );
            connection.Start();
            return connection;
        }

        #endregion

    }
}

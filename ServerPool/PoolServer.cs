using Grpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.ServerPooling
{
    /// <summary>
    /// Denotes the default implementation of a server pool
    /// </summary>
    [Export(typeof(IServerPool))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class PoolServer : IServerPool
    {

        private readonly ILoggingChannel _logger;
        private readonly ILoggingChannelFactory _loggingChannelFactory;
        private readonly ServerCredentials _credentials;
        private readonly ConcurrentBag<ServerData> _pooledServers = new ConcurrentBag<ServerData>();

        private static readonly IClientExecutionManager _discoveryExecutionManager = new DiscoveryExecutionManager();

        /// <summary>
        /// Initialize a new pool server with default credentials
        /// </summary>
        /// <param name="loggingChannelFactory">The logging channel factory to be used for logging</param>
        [ImportingConstructor]
        public PoolServer(ILoggingChannelFactory loggingChannelFactory)
        {
            _loggingChannelFactory = loggingChannelFactory;
            _logger = loggingChannelFactory.CreateChannel( typeof( PoolServer ) );

            GrpcServer = new Grpc.Core.Server();

            if(File.Exists( "server.crt" ) && File.Exists( "ca.crt" ) && File.Exists( "server.key" ))
            {
                var cacert = File.ReadAllText( @"ca.crt" );
                var servercert = File.ReadAllText( @"server.crt" );
                var serverkey = File.ReadAllText( @"server.key" );
                var keypair = new KeyCertificatePair( servercert, serverkey );
                _logger.Log( "Found certificates, starting server with TLS.".ToString, Severity.Info );
                _credentials = new SslServerCredentials( new List<KeyCertificatePair>() { keypair }, cacert, SslClientCertificateRequestType.DontRequest );
            }
            else
            {
                _logger.Log( $"No certificates found in folder {Environment.CurrentDirectory}. Server will start with plain-text communication".ToString, Severity.Warning );
                _credentials = ServerCredentials.Insecure;
            }

            GrpcServer.Services.Add( CreateServiceDefinition() );
        }

        /// <summary>
        /// Initialize a new pool server with default credentials
        /// </summary>
        /// <param name="loggingChannelFactory">The logging channel factory to be used for logging</param>
        /// <param name="server">The server this pool server should run on</param>
        /// <param name="credentials">The server credentials the pool server should use</param>
        public PoolServer(Grpc.Core.Server server, ServerCredentials credentials, ILoggingChannelFactory loggingChannelFactory)
        {

            _loggingChannelFactory = loggingChannelFactory;
            _logger = loggingChannelFactory.CreateChannel( typeof( PoolServer ) );

            GrpcServer = server;
            _credentials = credentials;

            GrpcServer.Services.Add( CreateServiceDefinition() );
        }

        /// <summary>The gRPC server.</summary>
        public Grpc.Core.Server GrpcServer { get; }

        /// <inheritdoc />
        public IEnumerable<ServerData> ConnectedServers => _pooledServers;

        /// <inheritdoc />
        public event EventHandler<ServerDataEventArgs> ServerConnected;

        /// <inheritdoc />
        public Task Shutdown()
        {
            return GrpcServer.ShutdownAsync();
        }

        /// <inheritdoc />
        public void Start( string host, int port)
        {
            if(!GrpcServer.Ports.Any( p => p.Host == host && p.Port == port ))
            {
                GrpcServer.Ports.Add( new ServerPort( host, port, _credentials ) );
            }
            GrpcServer.Start();
        }

        private ServerServiceDefinition CreateServiceDefinition()
        {
            ServerServiceDefinition.Builder builder = ServerServiceDefinition.CreateBuilder();
            builder.AddMethod( PoolingConstants.ConnectSilaServerMethod, ConnectServer );
            return builder.Build();
        }

        private Task ConnectServer( IAsyncStreamReader<SilaServerMessage> requestStream, IServerStreamWriter<SilaClientMessage> responseStream, ServerCallContext context )
        {
            var channel = new ServerConnection( _loggingChannelFactory, responseStream, requestStream, context.CancellationToken );
            var runTask = channel.Run();
            var server = CreateServerData( channel );
            _pooledServers.Add( server );
            ServerConnected?.Invoke( this, new ServerDataEventArgs( server ) );
            return runTask;
        }


        private ServerData CreateServerData(IClientChannel channel)
        {
            var silaService = new SiLAServiceClient( channel, _discoveryExecutionManager );
            var config = GetServerConfig( silaService );
            var info = GetServerInfo( silaService );
            var features = GetImplementedFeatures( silaService );
            var server = new ServerData( config, info, features, channel );
            return server;
        }

        private static List<Feature> GetImplementedFeatures( ISiLAService silaService )
        {
            return silaService.ImplementedFeatures.Select( f => FeatureSerializer.LoadFromXml( silaService.GetFeatureDefinition( f ) ) ).ToList();
        }

        private static ServerConfig GetServerConfig( ISiLAService silaService )
        {
            var name = silaService.ServerName;
            var uuid = Guid.Parse( silaService.ServerUUID );
            var config = new ServerConfig( name, uuid );
            return config;
        }

        private static ServerInformation GetServerInfo( ISiLAService silaService )
        {
            var type = silaService.ServerType;
            var description = silaService.ServerDescription;
            var vendorUri = silaService.ServerVendorURL;
            var version = silaService.ServerVersion;
            var info = new ServerInformation( type, description, vendorUri, version );
            return info;
        }

    }
}

using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ServerPooling
{
    /// <summary>
    /// Denotes the default implementation of a server pool
    /// </summary>
    [Export( typeof( IServerPool ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    public class PoolServer : IServerPool, IServiceHandler<IServerPool, ServiceMethodProviderContext<IServerPool>>
    {
        private readonly ConcurrentBag<ServerData> _pooledServers = new ConcurrentBag<ServerData>();

        private static readonly IClientExecutionManager _discoveryExecutionManager = new DiscoveryExecutionManager();

        /// <summary>
        /// Initialize a new pool server with default credentials
        /// </summary>
        /// <param name="serverProvider">The actual gRPC server</param>
        [ImportingConstructor]
        public PoolServer( IServiceConfigurationBuilder<IServerPool, ServiceMethodProviderContext<IServerPool>> serverProvider )
        {
            serverProvider.AddServiceHandler( this );
        }

        /// <inheritdoc />
        public IEnumerable<ServerData> ConnectedServers => _pooledServers;

        /// <inheritdoc />
        public event EventHandler<ServerDataEventArgs> ServerConnected;

        private Task ConnectServer( IAsyncStreamReader<SilaServerMessage> requestStream, IServerStreamWriter<SilaClientMessage> responseStream, ServerCallContext context )
        {
            var bufferedWriter = new BufferedStreamWriter<SilaClientMessage>(responseStream);
            var channel = new ServerConnection(bufferedWriter, requestStream, context.CancellationToken);
            var runTask = Task.WhenAll(channel.Run(), bufferedWriter.StartWriting(channel.CancellationToken));
            X509Certificate2 certificate = ExtractCertificate(context);
            var server = CreateServerData(channel, certificate, context.Peer);
            _pooledServers.Add(server);
            ServerConnected?.Invoke(this, new ServerDataEventArgs(server));
            return runTask;
        }

        private static X509Certificate2 ExtractCertificate(ServerCallContext context)
        {
            try
            {
                return context.GetHttpContext()?.Connection?.ClientCertificate;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private ServerData CreateServerData( IClientChannel channel, X509Certificate2 serverCertificate, string peer )
        {
            var silaService = new SiLAServiceClient( channel, _discoveryExecutionManager );
            var config = GetServerConfig( silaService );
            var info = GetServerInfo( silaService );
            var features = GetImplementedFeatures( silaService );
            var server = new ServerData( config, info, features, channel, serverCertificate, peer );
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

        /// <inheritdoc />
        public void Register( ServiceMethodProviderContext<IServerPool> context )
        {
            context.AddDuplexStreamingMethod( PoolingConstants.ConnectSilaServerMethod, new List<object>(), ConnectServer );
        }

        private Task ConnectServer( IServerPool server, IAsyncStreamReader<SilaServerMessage> requestStream, IServerStreamWriter<SilaClientMessage> responseStream, ServerCallContext context )
        {
            return ConnectServer( requestStream, responseStream, context );
        }
    }
}

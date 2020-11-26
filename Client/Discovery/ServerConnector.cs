using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Grpc.Core;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Discovery
{
    /// <summary>
    /// Standard implementation of a channel creator that creates insecure channels
    /// </summary>
    public class ServerConnector : IServerConnector
    {
        private readonly IClientExecutionManager _executionManager;
        private readonly ILoggingChannel _loggingChannel;
        private readonly SslCredentials _sslCredentials = new SslCredentials();

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="executionManager">A client execution manager</param>
        /// <param name="loggingChannelFactory">A factory used for logging</param>
        public ServerConnector( IClientExecutionManager executionManager, ILoggingChannelFactory loggingChannelFactory )
        {
            _executionManager = executionManager ?? new DiscoveryExecutionManager();
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof(ServerConnector) );
        }

        private Channel CreateChannel( string target, bool secureConnection )
        {
            if( secureConnection )
            {
                return new Channel(target, _sslCredentials);
            }
            return new Channel(target, ChannelCredentials.Insecure);
        }

        /// <inheritdoc />
        public virtual ServerData Connect( IPAddress host, int port )
        {
            return Connect( $"{host}:{port}" );
        }

        private ServerData Connect(string target)
        {
            try
            {
                _loggingChannel.Log($"Connecting to {target}".ToString, Severity.Info);
                return GetServerData(CreateChannel(target, true));
            }
            catch (Exception e)
            {
                _loggingChannel.LogException(
                    $"Secure connection to {target} failed, falling back to unsecure connection".ToString, e);
                return GetServerData(CreateChannel(target, false));
            }
        }

        /// <inheritdoc />
        public virtual ServerData Connect( string host, int port )
        {
            return Connect( $"{host}:{port}" );
        }


        /// <summary>
        /// Load all server properties 
        /// </summary>
        public ServerData GetServerData( Channel channel )
        {
            var clientChannel = new GrpcChannel( channel );
            var silaService = new SiLAServiceClient( clientChannel, _executionManager );
            var config = GetServerConfig( silaService );
            var info = GetServerInfo( silaService );
            var features = GetImplementedFeatures( silaService );
            var server = new ServerData( config, info, features, clientChannel );
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

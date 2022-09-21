using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Tecan.Sila2.AuthorizationProvider;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a public base class for the configuration of authentication and authorization
    /// </summary>
    public abstract class AuthConfigurationProviderBase : IAuthConfigurationProvider
    {
        private readonly IServerDiscovery _discovery;
        private readonly AuthConfiguration _configuration;
        private readonly IServerConnector _serverConnector;

        private const string ProviderFeatureId = "org.silastandard/core/AuthorizationProviderService/v1";

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="serverConnector">A component that can connect to servers</param>
        /// <param name="discovery">The discovery that should be used by this auth config</param>
        protected AuthConfigurationProviderBase( IServerDiscovery discovery, IServerConnector serverConnector )
        {
            _discovery = discovery;
            _serverConnector = serverConnector;
            _configuration = LoadConfiguration();

            try
            {
                if(_configuration.AuthProvider is DiscoveredServer discovered)
                {
                    SetAuthorizationProvider( discovered.ServerUUID );
                }
                else if(_configuration.AuthProvider is ExplicitServer explicitServer)
                {
                    SetAuthorizationProvider( explicitServer.Host, int.Parse( explicitServer.Port ) );
                }
            }
            catch(Exception e)
            {
                LogManager.GetLogger<AuthConfigurationProviderBase>().Error( "Restoring authorization provider failed", e );
            }
        }

        /// <summary>
        /// Loads the auth configuration from a preconfigured source
        /// </summary>
        /// <returns>A parsed auth configuration</returns>
        protected abstract AuthConfiguration LoadConfiguration();

        /// <inheritdoc />
        public User Login( string username, string password )
        {
            return Login( username, password, _configuration );
        }

        /// <summary>
        /// Logs in with the current credentials
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="password">The entered password</param>
        /// <param name="configuration">The current authentication configuration</param>
        /// <returns>A user or null, if the provided credentials are invalid</returns>
        protected abstract User Login( string username, string password, AuthConfiguration configuration );

        /// <inheritdoc />
        public IAuthorizationProviderService AuthorizationProvider
        {
            get;
            private set;
        }

        /// <inheritdoc />
        public string AuthorizationProviderUuid
        {
            get;
            private set;
        }

        /// <inheritdoc />
        public void SetAuthorizationProvider( string serverUuid )
        {
            SetAuthorizationProviderCore( serverUuid );
            _configuration.AuthProvider = new DiscoveredServer()
            {
                ServerUUID = serverUuid
            };
            SaveConfiguration( _configuration );
        }

        private void SetAuthorizationProviderCore( string serverUuid )
        {
            if(!Guid.TryParse( serverUuid, out var serverId ))
            {
                throw new ArgumentException( "The provided server is not a valid server id.", nameof( serverUuid ) );
            }
            if(_discovery == null)
            {
                throw new InvalidOperationException( "The server is not configured to discover authorization providers through a server uuid." );
            }

            var servers = _discovery.GetServers( TimeSpan.FromSeconds( 10 ) );
            var server = servers?.FirstOrDefault( s => s.Config.Uuid == serverId );
            if(server == null)
            {
                throw new ArgumentException( $"The server with id {serverUuid} could not be found.", nameof( serverUuid ) );
            }

            if(server.Features.All( f => f.FullyQualifiedIdentifier != ProviderFeatureId ))
            {
                throw new ArgumentException( "The server is not an authorization provider.", nameof( serverUuid ) );
            }

            AuthorizationProviderUuid = serverUuid;
            AuthorizationProvider =
                new AuthorizationProviderServiceClient( server.Channel, new DiscoveryExecutionManager() );
        }

        /// <summary>
        /// Saves the configuration to an appropriate location
        /// </summary>
        /// <param name="configuration">The auth configuration to save</param>
        protected abstract void SaveConfiguration( AuthConfiguration configuration );

        /// <inheritdoc />
        public void SetAuthorizationProvider( string host, int port )
        {
            SetAuthorizationProviderCore( host, port );
            _configuration.AuthProvider = new ExplicitServer()
            {
                Host = host,
                Port = port.ToString()
            };
            SaveConfiguration( _configuration );
        }

        private void SetAuthorizationProviderCore( string host, int port )
        {
            if(_serverConnector == null)
            {
                throw new InvalidOperationException( "The server is not configured to connect with a server with host name and port." );
            }
            var server = _serverConnector.Connect( host, port );
            if(server == null || server.Features.All( f => f.FullyQualifiedIdentifier != ProviderFeatureId ))
            {
                throw new ArgumentException( "The server is not an authorization provider.", nameof( host ) );
            }

            AuthorizationProviderUuid = server.Config.Uuid.ToString();
            AuthorizationProvider =
                new AuthorizationProviderServiceClient( server.Channel, new DiscoveryExecutionManager() );
        }
    }
}

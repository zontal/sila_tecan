using Common.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using Tecan.Sila2.Authentication.AuthenticationService;
using Tecan.Sila2.Authorization.AuthorizationConfigurationService;
using Tecan.Sila2.Client;
using Exception = System.Exception;

namespace Tecan.Sila2.Authentication
{
    /// <summary>
    /// Create interceptor for authentication
    /// </summary>
    [Export(typeof(IClientRequestInterceptor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class AuthenticationInterceptor : IClientRequestInterceptor
    {
        /// <inheritdoc />
        public string MetadataIdentifier => "org.silastandard/core/AuthorizationService/v1/Metadata/AccessToken";
        private readonly IAuthenticationHandler _authenticationHandler;
        internal static readonly ILog LoggingChannel = LogManager.GetLogger<AuthenticationInterceptor>();
        private readonly IServerDiscovery _discovery;
        private readonly Lazy<IExecutionManagerFactory> _executionManagerFactory;

        private const string AuthenticationServiceIdentifier = "org.silastandard/core/AuthenticationService/v1";
        private const string AuthorizationConfigurationServiceIdentifier = "org.silastandard/core/AuthorizationConfigurationService/v1";

        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="authenticationHandler">An object that handles the authentication internally</param>
        /// <param name="discovery">A server discovery agent</param>
        /// <param name="executionManagerFactory">A factory to create client execution managers</param>
        public AuthenticationInterceptor( IAuthenticationHandler authenticationHandler, IServerDiscovery discovery, Lazy<IExecutionManagerFactory> executionManagerFactory )
        {
            _authenticationHandler = authenticationHandler;
            _discovery = discovery;
            _executionManagerFactory = executionManagerFactory;
        }

        /// <inheritdoc />
        public IClientRequestInterception Intercept( ServerData server, string interceptedCommand, IClientExecutionManager executionManager, IDictionary<string, byte[]> metadata )
        {
            if(server == null )
            {
                return null;
            }
            var token = AuthenticationScope.Current.GetToken( server, s => CreateCachedToken( s, executionManager, interceptedCommand ) );
            if (token.Expiry < DateTime.Now)
            {
                token = Renew( server, token, executionManager, interceptedCommand );
            }
            metadata.Add( MetadataIdentifier, ByteSerializer.ToByteArray( new PropertyResponse<StringDto>( token.Token ), FrameworkTypeModel.Instance ) );
            return null;
        }

        private AuthTokenHandle Renew( ServerData server, AuthTokenHandle authTokenHandle, IClientExecutionManager executionManager, string interceptedCommand)
        {
            lock( authTokenHandle )
            {
                if( authTokenHandle.Expiry > DateTime.Now )
                {
                    return authTokenHandle;
                }

                if( authTokenHandle.Renew( _authenticationHandler, server ) )
                {
                    return authTokenHandle;
                }

                return AuthenticationScope.Current.GetToken( server, s => CreateCachedToken( s, executionManager, interceptedCommand ) );
            }
        }

        internal static DateTime GetExpiry( long tokenLifetime )
        {
            return DateTime.Now + TimeSpan.FromSeconds( tokenLifetime );
        }

        private AuthTokenHandle CreateCachedToken( ServerData server, IClientExecutionManager executionManager, string interceptedCommand)
        {
            if( server.Features.Any( f => f.FullyQualifiedIdentifier == AuthorizationConfigurationServiceIdentifier ) )
            {
                try
                {
                    var client = new AuthorizationConfigurationServiceClient( server.Channel, executionManager );
                    var currentProvider = client.AuthorizationProvider;
                    if (!string.IsNullOrEmpty(currentProvider)
                        && Guid.TryParse(currentProvider, out var providerUuid)
                        && (_authenticationHandler.AuthorizationProvider == null || providerUuid != _authenticationHandler.AuthorizationProvider.Config.Uuid))
                    {
                        var requestedFeatures = _authenticationHandler.GetFeaturesToRequest(server, interceptedCommand);
                        if (TryAuthenticateWithProvider(server, providerUuid, requestedFeatures, out var providerToken))
                        {
                            return providerToken;
                        }
                    }

                    if (_authenticationHandler.AuthorizationProvider != null && TryAuthenticateIntegrated(server, client, interceptedCommand, out var integratedToken))
                    {
                        return integratedToken;
                    }
                }
                catch( Exception exception )
                {
                    LoggingChannel.Error("Authentication using integrated authentication failed.", exception);
                }
            }

            if(server.Features.Any( f => f.FullyQualifiedIdentifier == AuthenticationServiceIdentifier ))
            {
                var requestedFeatures = _authenticationHandler.GetFeaturesToRequest(server, interceptedCommand);
                if (TryAuthenticateUserNamePassword( server, executionManager, requestedFeatures, out var userNamePasswordToken ))
                {
                    return userNamePasswordToken;
                }
            }

            throw new InvalidOperationException("Could not authenticate the request.");
        }

        private bool TryAuthenticateIntegrated(ServerData server, AuthorizationConfigurationServiceClient client, string interceptedCommand,
            out AuthTokenHandle authenticationTokenHandle)
        {
            var credentials = _authenticationHandler.CreateAuthorizationToken(server, interceptedCommand);
            if (credentials.HasValue)
            {
                LoggingChannel.Info($"Using integrated authentication for server {server.Config.Name}.");
                var providerString = _authenticationHandler.AuthorizationProvider.Config.Uuid.ToString("D");
                if (client.AuthorizationProvider != providerString)
                {
                    client.SetAuthorizationProvider(providerString);
                }

                {
                    authenticationTokenHandle = new IntegratedTokenHandle(credentials.Value.AccessToken, GetExpiry(credentials.Value.TokenLifetime), interceptedCommand);
                    return true;
                }
            }

            authenticationTokenHandle = null;
            return false;
        }

        private bool TryAuthenticateWithProvider(ServerData server, Guid providerUuid, string[] features, out AuthTokenHandle authenticationTokenHandle)
        {
            var provider = _discovery.Connect( providerUuid, TimeSpan.FromSeconds( 1 ) );
            if( provider == null )
            {
                authenticationTokenHandle = null;
                return false;
            }
            if (provider.Features.Any(f => f.FullyQualifiedIdentifier == AuthenticationServiceIdentifier))
            {
                var credentials = _authenticationHandler.GetCredentials(server);
                if (credentials != null)
                {
                    LoggingChannel.Info($"Authenticating with provider {provider.Config.Name} using username and password.");
                    var providerClient = new AuthenticationServiceClient( provider.Channel, _executionManagerFactory.Value.CreateExecutionManager(provider) );
                    try
                    {
                        var response = providerClient.Login(credentials.Username, credentials.Password,
                            server.Config.Uuid.ToString("D"), features);
                        
                        authenticationTokenHandle = new UserNamePasswordTokenHandle(response.AccessToken, GetExpiry(response.TokenLifetime), credentials.Username,
                                credentials.Password, providerClient, features);
                        
                        return true;
                    }
                    catch (Exception exception)
                    {
                        LoggingChannel.Error("Authentication failed", exception);
                    }
                }
            }
            authenticationTokenHandle = null;
            return false;
        }

        private bool TryAuthenticateUserNamePassword( ServerData server, IClientExecutionManager executionManager, string[] features, out AuthTokenHandle tokenHandle)
        {
            var credentials = _authenticationHandler.GetCredentials(server);
            if (credentials != null)
            {
                LoggingChannel.Info($"Authenticating with {server.Config.Name} using username and password.");
                var client = new AuthenticationServiceClient( server.Channel, executionManager );
                try
                {
                    var response = client.Login(credentials.Username, credentials.Password, server.Config.Uuid.ToString("D"),
                        features);
                    {
                        tokenHandle = new UserNamePasswordTokenHandle(response.AccessToken, GetExpiry(response.TokenLifetime), credentials.Username,
                            credentials.Password, client, features);
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    LoggingChannel.Error("Authentication failed", exception);
                }
            }
            tokenHandle = null;
            return false;
        }
    }
}

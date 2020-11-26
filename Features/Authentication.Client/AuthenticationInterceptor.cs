using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using Tecan.MAP.Contracts.Services.Logging;
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
        private readonly ILoggingChannel _loggingChannel;
        private readonly Lazy<IConnectionManager> _connectionManager;
        private readonly Lazy<IExecutionManagerFactory> _executionManagerFactory;
        private readonly ConditionalWeakTable<ServerData, AuthTokenHandle> _cachedTokens = new ConditionalWeakTable<ServerData, AuthTokenHandle>();

        private const string AuthenticationServiceIdentifier = "org.silastandard/core/AuthenticationService/v1";
        private const string AuthorizationConfigurationServiceIdentifier = "org.silastandard/core/AuthorizationConfigurationService/v1";

        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="authenticationHandler">An object that handles the authentication internally</param>
        /// <param name="loggingChannelFactory">A logging channel factory</param>
        /// <param name="connectionManager">A connection manager</param>
        /// <param name="executionManagerFactory">A factory to create client execution managers</param>
        public AuthenticationInterceptor( IAuthenticationHandler authenticationHandler, ILoggingChannelFactory loggingChannelFactory, Lazy<IConnectionManager> connectionManager, Lazy<IExecutionManagerFactory> executionManagerFactory )
        {
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof(AuthenticationInterceptor) );
            _authenticationHandler = authenticationHandler;
            _connectionManager = connectionManager;
            _executionManagerFactory = executionManagerFactory;
        }

        /// <inheritdoc />
        public IClientRequestInterception Intercept( ServerData server, IClientExecutionManager executionManager, IDictionary<string, byte[]> metadata )
        {
            if(server == null )
            {
                return null;
            }
            var token = _cachedTokens.GetValue( server, s => CreateCachedToken( s, executionManager ) );
            if (token.Expiry < DateTime.Now)
            {
                token = Renew( server, token, executionManager );
            }
            metadata.Add( MetadataIdentifier, ByteSerializer.ToByteArray<StringDto>( token.Token ) );
            return null;
        }

        private AuthTokenHandle Renew( ServerData server, AuthTokenHandle authTokenHandle, IClientExecutionManager executionManager )
        {
            lock( authTokenHandle )
            {
                if( authTokenHandle.Expiry > DateTime.Now )
                {
                    return authTokenHandle;
                }

                if( authTokenHandle.Renew( this, server ) )
                {
                    return authTokenHandle;
                }

                _cachedTokens.Remove( server );
                return _cachedTokens.GetValue( server, s => CreateCachedToken( s, executionManager ) );
            }
        }

        private static DateTime GetExpiry( long tokenLifetime )
        {
            return DateTime.Now + TimeSpan.FromSeconds( tokenLifetime );
        }

        private AuthTokenHandle CreateCachedToken( ServerData server, IClientExecutionManager executionManager )
        {
            if( server.Features.Any( f => f.FullyQualifiedIdentifier == AuthorizationConfigurationServiceIdentifier ) )
            {
                try
                {
                    var client = new AuthorizationConfigurationServiceClient( server.Channel, executionManager );
                    var currentProvider = client.AuthorizationProvider;
                    if( !string.IsNullOrEmpty( currentProvider ) && Guid.TryParse( currentProvider, out var providerUuid ) )
                    {
                        if( _authenticationHandler.AuthorizationProvider == null || providerUuid != _authenticationHandler.AuthorizationProvider.Config.Uuid )
                        {
                            if( TryAuthenticateWithProvider( server, providerUuid, out var providerToken ) )
                            {
                                return providerToken;
                            }
                        }
                    }

                    if( _authenticationHandler.AuthorizationProvider != null )
                    {
                        if( TryAuthenticateIntegrated( server, client, out var integratedToken ) )
                        {
                            return integratedToken;
                        }
                    }
                }
                catch( Exception exception )
                {
                    _loggingChannel.LogException("Authentication using integrated authentication failed.".ToString, exception);
                }
            }

            if(server.Features.Any( f => f.FullyQualifiedIdentifier == AuthenticationServiceIdentifier ))
            {
                if(TryAuthenticateUserNamePassword( server, executionManager, out var userNamePasswordToken ))
                {
                    return userNamePasswordToken;
                }
            }

            throw new InvalidOperationException("Could not authenticate the request.");
        }

        private bool TryAuthenticateIntegrated(ServerData server, AuthorizationConfigurationServiceClient client,
            out AuthTokenHandle authenticationTokenHandle)
        {
            var credentials = _authenticationHandler.CreateAuthorizationToken(server);
            if (credentials.HasValue)
            {
                _loggingChannel.Log($"Using integrated authentication for server {server.Config.Name}.".ToString,
                    Severity.Info);
                var providerString = _authenticationHandler.AuthorizationProvider.Config.Uuid.ToString("D");
                if (client.AuthorizationProvider != providerString)
                {
                    client.SetAuthorizationProvider(providerString);
                }

                {
                    authenticationTokenHandle = new IntegratedTokenHandle(credentials.Value.AccessToken, GetExpiry(credentials.Value.TokenLifetime));
                    return true;
                }
            }

            authenticationTokenHandle = null;
            return false;
        }

        private bool TryAuthenticateWithProvider(ServerData server, Guid providerUuid, out AuthTokenHandle authenticationTokenHandle)
        {
            var provider = _connectionManager.Value.Connect(providerUuid, TimeSpan.FromSeconds(10));
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
                    _loggingChannel.Log($"Authenticating with provider {provider.Config.Name} using username and password.".ToString,
                        Severity.Info);
                    var providerClient = new AuthenticationServiceClient( provider.Channel, _executionManagerFactory.Value.CreateExecutionManager(provider) );
                    try
                    {
                        var response = providerClient.Login(credentials.Username, credentials.Password,
                            server.Config.Uuid.ToString("D"), Array.Empty<string>());
                        {
                            authenticationTokenHandle = new UserNamePasswordTokenHandle(response.AccessToken, GetExpiry(response.TokenLifetime), credentials.Username,
                                credentials.Password, providerClient);
                            return true;
                        }
                    }
                    catch (Exception exception)
                    {
                        _loggingChannel.LogException("Authentication failed".ToString, exception);
                    }
                }
            }
            authenticationTokenHandle = null;
            return false;
        }

        private bool TryAuthenticateUserNamePassword( ServerData server, IClientExecutionManager executionManager, out AuthTokenHandle tokenHandle)
        {
            var credentials = _authenticationHandler.GetCredentials(server);
            if (credentials != null)
            {
                _loggingChannel.Log($"Authenticating with {server.Config.Name} using username and password.".ToString,
                    Severity.Info);
                var client = new AuthenticationServiceClient( server.Channel, executionManager );
                try
                {
                    var response = client.Login(credentials.Username, credentials.Password, server.Config.Uuid.ToString("D"),
                        Array.Empty<string>());
                    {
                        tokenHandle = new UserNamePasswordTokenHandle(response.AccessToken, GetExpiry(response.TokenLifetime), credentials.Username,
                            credentials.Password, client);
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    _loggingChannel.LogException("Authentication failed".ToString, exception);
                }
            }
            tokenHandle = null;
            return false;
        }

        private abstract class AuthTokenHandle
        {
            public string Token
            {
                get;
                protected set;
            }

            public DateTime Expiry
            {
                get;
                protected set;
            }

            public abstract bool Renew( AuthenticationInterceptor parent, ServerData server );

            protected AuthTokenHandle( string token, DateTime expiry )
            {
                Token = token;
                Expiry = expiry;
            }
        }

        private class IntegratedTokenHandle : AuthTokenHandle
        {
            public IntegratedTokenHandle(string token, DateTime expiry) : base(token, expiry)
            {
            }

            public override bool Renew( AuthenticationInterceptor parent, ServerData server )
            {
                var credentials = parent._authenticationHandler.CreateAuthorizationToken( server );
                if( credentials.HasValue )
                {
                    Token = credentials.Value.AccessToken;
                    Expiry = GetExpiry( credentials.Value.TokenLifetime );
                    return true;
                }

                return false;
            }
        }

        private class UserNamePasswordTokenHandle : AuthTokenHandle
        {

            public UserNamePasswordTokenHandle( string token, DateTime expiry, string userName, string password, IAuthenticationService authenticationService )
                : base(token, expiry)
            {
                UserName = userName;
                Password = password;
                _authenticationService = authenticationService;
            }

            private string UserName
            {
                get;
                set;
            }

            private string Password
            {
                get;
                set;
            }

            private readonly IAuthenticationService _authenticationService;

            public override bool Renew( AuthenticationInterceptor parent, ServerData server )
            {
                try
                {
                    var login = _authenticationService.Login( UserName, Password, server.Config.Uuid.ToString( "D" ), Array.Empty<string>() );
                    Token = login.AccessToken;
                    Expiry = GetExpiry( login.TokenLifetime );
                    return true;
                }
                catch( Exception exception )
                {
                    parent._loggingChannel.LogException( "Authentication failed, retry with fresh credentials.".ToString, exception );
                    var newCredentials = parent._authenticationHandler.GetCredentials( server );
                    try
                    {
                        var newLoginAttempt =
                            _authenticationService.Login( newCredentials.Username, newCredentials.Password, server.Config.Uuid.ToString( "D" ), Array.Empty<string>() );
                        UserName = newCredentials.Username;
                        Password = newCredentials.Password;
                        Token = newLoginAttempt.AccessToken;
                        Expiry = GetExpiry( newLoginAttempt.TokenLifetime );
                        return true;
                    }
                    catch( Exception newAttemptException )
                    {
                        parent._loggingChannel.LogException("New authentication attempt failed as well. Falling back to old credentials.".ToString, newAttemptException);
                        return false;
                    }
                }
            }
        }
    }
}

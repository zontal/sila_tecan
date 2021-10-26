using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Tecan.Sila2.Authentication;
using Tecan.Sila2.Authorization;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Server;
using InvalidAccessTokenException = Tecan.Sila2.Authorization.InvalidAccessTokenException;
using InvalidProviderAccessTokenException = Tecan.Sila2.AuthorizationProvider.InvalidAccessTokenException;

namespace Tecan.Sila2
{
    [Export(typeof(IAuthorizationService))]
    [Export(typeof(IAuthenticationService))]
    [Export(typeof(IAuthorizationConfigurationService))]
    [Export(typeof(IRequestInterceptor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AuthenticationAuthorizationService : AccessTokenInterceptorBase, IAuthorizationService, IAuthenticationService, IAuthorizationConfigurationService, IRequestInterceptor
    {
        private readonly IAuthConfigurationProvider _configurationProvider;
        private readonly Lazy<ISiLAService> _silaService;

        [ImportingConstructor]
        public AuthenticationAuthorizationService( Lazy<ISiLAService> silaService, [ImportMany] IEnumerable<IAuthConfigurationProvider> configurationProviders, IServerConnector serverConnector, DefaultConfigurationProvider defaultProvider)
        {
            _silaService = silaService;
            _configurationProvider = configurationProviders?.SingleOrDefault() ?? defaultProvider;
        }

        private readonly ConcurrentDictionary<string, AuthToken> _currentAuthorizations = new ConcurrentDictionary<string, AuthToken>();

        public IRequestInterceptor AccessToken => this;

        public override int Priority => 3;

        public override bool AppliesToCommands => true;

        public override bool AppliesToProperties => true;

        public override bool IsInterceptRequired(Feature feature, string commandIdentifier)
        {
            return !commandIdentifier.StartsWith("org.silastandard/core/SiLAService/") && !commandIdentifier.StartsWith("org.silastandard/core/AuthenticationService/v1/Command");
        }

        public override IRequestInterception Intercept(string commandIdentifier, string accessToken)
        {
            if (accessToken == null)
            {
                throw new InvalidAccessTokenException( "The access token is missing" );
            }
            if (!_currentAuthorizations.TryGetValue(accessToken, out var token))
            {
                if( _configurationProvider.AuthorizationProvider != null )
                {
                    try
                    {
                        if( _configurationProvider.AuthorizationProvider.Verify( accessToken, _silaService.Value.ServerUUID, GetFeatureIdentifier(commandIdentifier) ) > 0 )
                        {
                            return null;
                        }
                    }
                    catch( InvalidProviderAccessTokenException e )
                    {
                        throw new InvalidAccessTokenException( e.Message );
                    }
                }

                throw new InvalidAccessTokenException($"The access token is not valid anymore." );
            }
            else if( token.Expiry < DateTime.Now )
            {
                throw new InvalidAccessTokenException( "The access token is not valid anymore." );
            }
            else if (!token.Allows(commandIdentifier))
            {
                throw new InvalidAccessTokenException( "The access token does not authorize the command." );
            }
            return null;
        }

        private string GetFeatureIdentifier( string commandIdentifier )
        {
            // both commands and properties end with /Command/ or /Property/, so take second last /
            var lastSlash = commandIdentifier.LastIndexOf( '/' );
            if( lastSlash != -1 )
            {
                var secondLastSlash = commandIdentifier.LastIndexOf( '/', lastSlash - 1 );
                if( secondLastSlash > 0 )
                {
                    return commandIdentifier.Substring( 0, secondLastSlash );
                }
            }

            return null;
        }

        public LoginResponse Login(string userIdentification, string password, string server, ICollection<string> requestedFeatures)
        {
            LoginResponse loginResponse;
            if (server != _silaService.Value.ServerUUID)
            {
                throw new AuthenticationFailedException($"Cannot provide access token for server {server}. This server can only serve authentication requests for server {_silaService.Value.ServerName} ({_silaService.Value.ServerUUID}).");
            }

            var user = _configurationProvider.LocalUsers.FirstOrDefault( u => u.Name == userIdentification && u.Password == password );
            if (user == null)
            {
                throw new AuthenticationFailedException("Given credentials are incorrect.");
            }
            else
            {
                string[] featureArray;
                if( requestedFeatures == null || requestedFeatures.Count == 0 )
                {
                    if( !user.AllowWildcard )
                    {
                        throw new AuthenticationFailedException("User does not have wildcard permission.");
                    }

                    featureArray = new string[] { "" };
                }
                else
                {
                    if( !user.AllowWildcard )
                    {
                        var failedPermissions = requestedFeatures.Except( user.Permission ).ToList();
                        if( failedPermissions.Count > 0 )
                        {
                            throw new AuthenticationFailedException($"User {userIdentification} does not have permissions for feature(s) {string.Join(", ", failedPermissions)}.");
                        }
                    }

                    featureArray = requestedFeatures.ToArray();
                }
                var expiry = (DateTime.Now.AddHours(24));
                var tempGuid = Guid.NewGuid().ToString();
                _currentAuthorizations[tempGuid] = new AuthToken(expiry, featureArray);
                loginResponse = new LoginResponse(tempGuid, 24 * 3600);
            }
            return loginResponse;
        }

        public void Logout(string accessToken)
        {
            if (accessToken == null || !_currentAuthorizations.TryGetValue(accessToken, out var token) || token.Expiry < DateTime.Now)
            {
                throw new InvalidAccessTokenException($"The sent AccessToken is not valid anymore.");
            }
            _currentAuthorizations.TryRemove(accessToken, out _);
        }


        public string AuthorizationProvider
        {
            get => _configurationProvider.AuthorizationProviderUuid;
        }

        public void SetAuthorizationProvider( string authorizationProvider )
        {
            _configurationProvider.SetAuthorizationProvider( authorizationProvider );
        }

        private class AuthToken
        {
            public AuthToken( DateTime expiry, string[] features )
            {
                Expiry = expiry;
                Features = features;
            }

            public bool Allows( string commandIdentifier )
            {
                return Features.Any( f => commandIdentifier.StartsWith( f ) );
            }

            public DateTime Expiry
            {
                get;
            }

            public string[] Features
            {
                get;
            }
        }
    }
}

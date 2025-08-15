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
    /// <summary>
    /// Denotes the default implementation of an authentication and authorization service
    /// </summary>
    [Export( typeof( IAuthorizationService ) )]
    [Export( typeof( IAuthenticationService ) )]
    [Export( typeof( IAuthorizationConfigurationService ) )]
    [Export( typeof( IRequestInterceptor ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    public class AuthenticationAuthorizationService : AccessTokenInterceptorBase, IAuthorizationService, IAuthenticationService, IAuthorizationConfigurationService, IRequestInterceptor
    {
        private readonly IAuthConfigurationProvider _configurationProvider;
        private readonly Lazy<ISiLAService> _silaService;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        [ImportingConstructor]
        public AuthenticationAuthorizationService( Lazy<ISiLAService> silaService, IAuthConfigurationProvider configurationProvider )
        {
            _silaService = silaService;
            _configurationProvider = configurationProvider;
        }

        private readonly ConcurrentDictionary<string, AuthToken> _currentAuthorizations = new ConcurrentDictionary<string, AuthToken>();

        /// <inheritdoc />
        public IRequestInterceptor AccessToken => this;

        /// <inheritdoc />
        public override int Priority => 3;

        /// <inheritdoc />
        public override bool AppliesToCommands => true;

        /// <inheritdoc />
        public override bool AppliesToProperties => true;

        /// <inheritdoc />
        public override bool IsInterceptRequired( Feature feature, string commandIdentifier )
        {
            return !commandIdentifier.StartsWith( "org.silastandard/core/SiLAService/" ) && !commandIdentifier.StartsWith( "org.silastandard/core/AuthenticationService/v1/Command" );
        }

        /// <inheritdoc />
        public override IRequestInterception Intercept( string commandIdentifier, string accessToken )
        {
            if(accessToken == null)
            {
                throw new InvalidAccessTokenException( "The access token is missing" );
            }
            if (_configurationProvider.IsValidApiKey(accessToken, commandIdentifier))
            {
                return null;
            }
            if(!_currentAuthorizations.TryGetValue( accessToken, out var token ))
            {
                if(_configurationProvider.AuthorizationProvider != null)
                {
                    try
                    {
                        if(_configurationProvider.AuthorizationProvider.Verify( accessToken, _silaService.Value.ServerUUID, GetFeatureIdentifier( commandIdentifier ) ) > 0)
                        {
                            return null;
                        }
                    }
                    catch(InvalidProviderAccessTokenException e)
                    {
                        throw new InvalidAccessTokenException( e.Message );
                    }
                }

                throw new InvalidAccessTokenException( $"The access token is not valid anymore." );
            }
            else if(token.Expiry < DateTime.Now)
            {
                throw new InvalidAccessTokenException( "The access token is not valid anymore." );
            }
            else if(!token.Allows( commandIdentifier ))
            {
                throw new InvalidAccessTokenException( "The access token does not authorize the command." );
            }
            return null;
        }

        private string GetFeatureIdentifier( string commandIdentifier )
        {
            // both commands and properties end with /Command/ or /Property/, so take second last /
            var lastSlash = commandIdentifier.LastIndexOf( '/' );
            if(lastSlash != -1)
            {
                var secondLastSlash = commandIdentifier.LastIndexOf( '/', lastSlash - 1 );
                if(secondLastSlash > 0)
                {
                    return commandIdentifier.Substring( 0, secondLastSlash );
                }
            }

            return null;
        }

        /// <inheritdoc />
        public LoginResponse Login( string userIdentification, string password, string requestedServer, ICollection<string> requestedFeatures )
        {
            if(requestedServer != _silaService.Value.ServerUUID)
            {
                throw new AuthenticationFailedException( $"Cannot provide access token for server {requestedServer}. This server can only serve authentication requests for server {_silaService.Value.ServerName} ({_silaService.Value.ServerUUID})." );
            }

            IdentifierConstraint.CheckParameters(requestedFeatures, IdentifierType.FeatureIdentifier, nameof(requestedFeatures));

            var user = _configurationProvider.Login( userIdentification, password );
            if(user == null)
            {
                throw new AuthenticationFailedException( "The provided credentials are incorrect." );
            }
            else
            {
                return LoginCore(userIdentification, requestedFeatures, user);
            }
        }

        private LoginResponse LoginCore(string userIdentification, ICollection<string> requestedFeatures, User user)
        {
            string[] featureArray;
            if (requestedFeatures == null || requestedFeatures.Count == 0)
            {
                if (!user.AllowWildcard)
                {
                    throw new AuthenticationFailedException("User does not have wildcard permission.");
                }

                featureArray = new string[] { "" };
            }
            else
            {
                if (!user.AllowWildcard)
                {
                    var failedPermissions = requestedFeatures.Except(user.Permission).ToList();
                    if (failedPermissions.Count > 0)
                    {
                        throw new AuthenticationFailedException($"User {userIdentification} does not have permissions for feature(s) {string.Join(", ", failedPermissions)}.");
                    }
                }

                featureArray = requestedFeatures.ToArray();
            }
            var expiry = (DateTime.Now.AddHours(24));
            var tempGuid = Guid.NewGuid().ToString();
            _currentAuthorizations[tempGuid] = new AuthToken(expiry, featureArray);
            return new LoginResponse(tempGuid, 24 * 3600);
        }

        /// <inheritdoc />
        public void Logout( string accessToken )
        {
            if(accessToken == null || !_currentAuthorizations.TryGetValue( accessToken, out var token ) || token.Expiry < DateTime.Now)
            {
                throw new Tecan.Sila2.Authentication.InvalidAccessTokenException( $"The sent AccessToken is not valid anymore." );
            }
            _currentAuthorizations.TryRemove( accessToken, out _ );
        }


        /// <inheritdoc />
        public string AuthorizationProvider
        {
            get => _configurationProvider.AuthorizationProviderUuid;
        }

        /// <inheritdoc />
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Tecan.Sila2.Authentication;
using Tecan.Sila2.Authorization;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Server;
using InvalidAccessTokenException = Tecan.Sila2.Authorization.InvalidAccessTokenException;

namespace Tecan.Sila2
{
    [Export(typeof(IAuthorizationService))]
    [Export(typeof(IAuthenticationService))]
    [Export(typeof(IRequestInterceptor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AuthenticationAuthorizationService : AccessTokenInterceptorBase, IAuthorizationService, IAuthenticationService, IRequestInterceptor
    {
        private string _userIdentification = "ExampleUser";
        private string _password = "ExamplePassword";
        private readonly Lazy<ISiLAService> _silaService;

        [ImportingConstructor]
        public AuthenticationAuthorizationService(Lazy<ISiLAService> silaService)
        {
            _silaService = silaService;
        }


        private readonly ConcurrentDictionary<string, DateTime> _currentAuthorizations = new ConcurrentDictionary<string, DateTime>();

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
            if (accessToken == null || !_currentAuthorizations.TryGetValue(accessToken, out var expiry) || expiry < DateTime.Now)
            {
                throw new InvalidAccessTokenException($"The sent AccessToken is not valid anymore.");
            }
            return null;
        }

        public LoginResponse Login(string userIdentification, string password, string server, ICollection<string> requestedFeatures)
        {
            LoginResponse loginResponse;
            if (server != _silaService.Value.ServerUUID)
            {
                throw new AuthenticationFailedException($"Cannot provider access token for server {server}. This server can only serve authentication requests for server {_silaService.Value.ServerName} ({_silaService.Value.ServerUUID}).");
            }
            
            if (!_userIdentification.Equals(userIdentification) && _password.Equals(password))
            {
                throw new AuthenticationFailedException("Given credentials are not authorized or incorrect.");
            }
            else
            {
                var expiry = (DateTime.Now.AddHours(24));
                var tempGuid = Guid.NewGuid().ToString();
                _currentAuthorizations[tempGuid] = expiry;
                loginResponse = new LoginResponse(tempGuid, expiry.Ticks);
            }
            return loginResponse;
        }

        public void Logout(string accessToken)
        {
            if (accessToken == null || !_currentAuthorizations.TryGetValue(accessToken, out var expiry) || expiry < DateTime.Now)
            {
                throw new InvalidAccessTokenException($"The sent AccessToken is not valid anymore.");
            }
            _currentAuthorizations.TryRemove(accessToken, out _);
        }



    }
}

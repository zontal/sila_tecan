using System;
using Exception = System.Exception;

namespace Tecan.Sila2.Authentication
{
    internal class UserNamePasswordTokenHandle : AuthTokenHandle
    {

        public UserNamePasswordTokenHandle(string token, DateTime expiry, string userName, string password, IAuthenticationService authenticationService, string[] features)
            : base(token, expiry)
        {
            UserName = userName;
            Password = password;
            _authenticationService = authenticationService;
            Features = features;
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

        private string[] Features
        {
            get;
            set;
        }

        private readonly IAuthenticationService _authenticationService;

        public override bool Renew(IAuthenticationHandler authHandler, ServerData server)
        {
            try
            {
                var login = _authenticationService.Login(UserName, Password, server.Config.Uuid.ToString("D"), Features);
                Token = login.AccessToken;
                Expiry = AuthenticationInterceptor.GetExpiry(login.TokenLifetime);
                return true;
            }
            catch (Exception exception)
            {
                AuthenticationInterceptor.LoggingChannel.Error("Authentication failed, retry with fresh credentials.", exception);
                var newCredentials = authHandler.GetCredentials(server);
                try
                {
                    var newLoginAttempt =
                        _authenticationService.Login(newCredentials.Username, newCredentials.Password, server.Config.Uuid.ToString("D"), Features);
                    UserName = newCredentials.Username;
                    Password = newCredentials.Password;
                    Token = newLoginAttempt.AccessToken;
                    Expiry = AuthenticationInterceptor.GetExpiry(newLoginAttempt.TokenLifetime);
                    return true;
                }
                catch (Exception newAttemptException)
                {
                    AuthenticationInterceptor.LoggingChannel.Error("New authentication attempt failed as well. Falling back to old credentials.", newAttemptException);
                    return false;
                }
            }
        }

        public override bool Logout()
        {
            _authenticationService.Logout(Token);
            return true;
        }
    }
}

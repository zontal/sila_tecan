using System;

namespace Tecan.Sila2.Authentication
{
    internal class IntegratedTokenHandle : AuthTokenHandle
    {
        private string RequestedCommand { get; set; }

        public IntegratedTokenHandle(string token, DateTime expiry, string requestedCommand) : base(token, expiry)
        {
            RequestedCommand = requestedCommand;
        }

        public override bool Renew(IAuthenticationHandler authHandler, ServerData server)
        {
            var credentials = authHandler.CreateAuthorizationToken(server, RequestedCommand);
            if (credentials.HasValue)
            {
                Token = credentials.Value.AccessToken;
                Expiry = AuthenticationInterceptor.GetExpiry(credentials.Value.TokenLifetime);
                return true;
            }

            return false;
        }

        public override bool Logout()
        {
            return false;
        }
    }
}

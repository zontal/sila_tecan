using System;

namespace Tecan.Sila2.Authentication
{
    internal abstract class AuthTokenHandle
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

        public abstract bool Renew(IAuthenticationHandler authHandler, ServerData server);

        public abstract bool Logout();

        protected AuthTokenHandle(string token, DateTime expiry)
        {
            Token = token;
            Expiry = expiry;
        }
    }
}

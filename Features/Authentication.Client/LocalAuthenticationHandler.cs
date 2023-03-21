using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Authentication
{
    /// <summary>
    /// Denotes a class that only supports local authentication
    /// </summary>
    public abstract class LocalAuthenticationHandler : IAuthenticationHandler
    {
        /// <inheritdoc />
        public ServerData AuthorizationProvider => null;

        /// <inheritdoc />
        public LoginResponse? CreateAuthorizationToken(ServerData server, string requestedCommand)
        {
            throw new NotSupportedException("This authentication handler does not support integrated authentication");
        }

        /// <inheritdoc />
        public abstract Credentials GetCredentials(ServerData server);

        /// <inheritdoc />
        public virtual string[] GetFeaturesToRequest(ServerData server, string requiredCommand)
        {
            var lastSep = requiredCommand.LastIndexOf('/');
            if (lastSep != -1)
            {
                var secondLastSep = requiredCommand.LastIndexOf('/', lastSep - 1);
                if (secondLastSep != -1)
                {
                    return new[]
                    {
                        requiredCommand.Substring(0, secondLastSep),
                    };
                }
            }
            return Array.Empty<string>();
        }
    }
}

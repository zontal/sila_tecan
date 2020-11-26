using System.Collections.Generic;
using Tecan.Sila2.AuthorizationProvider;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes an interface to configure the authentication system
    /// </summary>
    public interface IAuthConfigurationProvider
    {
        /// <summary>
        /// Gets the users registered locally with the device
        /// </summary>
        IEnumerable<User> LocalUsers
        {
            get;
        }

        /// <summary>
        /// Gets a client for the chosen authorization provider or null, if not set
        /// </summary>
        IAuthorizationProviderService AuthorizationProvider
        {
            get;
        }

        /// <summary>
        /// Gets the identifier of the current authorization provider
        /// </summary>
        string AuthorizationProviderUuid
        {
            get;
        }

        /// <summary>
        /// Sets the authorization provider to the provided server
        /// </summary>
        /// <param name="serverUuid">The universally unique identifier of the server</param>
        void SetAuthorizationProvider( string serverUuid );

        /// <summary>
        /// Sets the authorization provider to the provided server
        /// </summary>
        /// <param name="host">The hostname of the server</param>
        /// <param name="port">The port on which to reach the server</param>
        void SetAuthorizationProvider( string host, int port );
    }
}

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
        /// Logs in with the current credentials
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="password">The entered password</param>
        /// <returns>A user or null, if the provided credentials are invalid</returns>
        User Login( string username, string password );

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
        /// Determines whether the given access token is a valid API key
        /// </summary>
        /// <param name="apiKey">the API key</param>
        /// <param name="commandIdentifier">the command that was attempted to run</param>
        /// <returns>true, if the api key is valid for the fiven command identifier, otherwise false</returns>
        bool IsValidApiKey( string apiKey, string commandIdentifier );

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

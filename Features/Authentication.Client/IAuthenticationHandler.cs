namespace Tecan.Sila2.Authentication
{
    /// <summary>
    /// Denotes a component that handles the authentication of users
    /// </summary>
    public interface IAuthenticationHandler
    {
        /// <summary>
        /// Gets the saved credentials for a given server
        /// </summary>
        /// <param name="server">The server for which credentials are requested</param>
        /// <returns>Credentials or null, in case an integrated authorization provider is used.</returns>
        Credentials GetCredentials( ServerData server );

        /// <summary>
        /// Creates an authorization token for the given server, in case an integrated authorization provider exists and the authentication handler has access to it
        /// </summary>
        /// <param name="server">The server that should be connected to</param>
        /// <param name="requestedCommand">The fully qualified identifier of the command for which an access token is needed</param>
        /// <returns>A login response based on the current principal or null, if no integrated authorization provider is used or the user does not have access to the server.</returns>
        LoginResponse? CreateAuthorizationToken( ServerData server, string requestedCommand );

        /// <summary>
        /// Gets the integrated authorization provider or null, if no such server is used.
        /// </summary>
        ServerData AuthorizationProvider
        {
            get;
        }

        /// <summary>
        /// Gets an array of feature identifiers that should be requested
        /// </summary>
        /// <param name="server">The server for which to make the request</param>
        /// <param name="requiredCommand">The command that initially required an access token</param>
        /// <returns>A collection of feature identifiers</returns>
        string[] GetFeaturesToRequest(ServerData server, string requiredCommand);
    }
}

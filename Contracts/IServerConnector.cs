using System.Net;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes an interface for a component that creates channels
    /// </summary>
    public interface IServerConnector
    {
        /// <summary>
        /// Creates a channel to the given server
        /// </summary>
        /// <param name="host">The host that should be connected</param>
        /// <param name="port">The port on which the system should connect to</param>
        /// <returns>A server data object</returns>
        ServerData Connect( IPAddress host, int port );

        /// <summary>
        /// Creates a channel to the given server
        /// </summary>
        /// <param name="host">The host that should be connected</param>
        /// <param name="port">The port on which the system should connect to</param>
        /// <returns>A server data object</returns>
        ServerData Connect( string host, int port );
    }
}

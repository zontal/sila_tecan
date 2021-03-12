using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Creates a channel to the given server
        /// </summary>
        /// <param name="host">The host that should be connected</param>
        /// <param name="port">The port on which the system should connect to</param>
        /// <param name="expectedServer">The expected server UUID at the given location</param>
        /// <param name="details">Connection details provided by the server</param>
        /// <returns>A server data object</returns>
        ServerData Connect( IPAddress host, int port, Guid? expectedServer, IReadOnlyDictionary<string, string> details );

        /// <summary>
        /// Creates a channel to the given server
        /// </summary>
        /// <param name="host">The host that should be connected</param>
        /// <param name="port">The port on which the system should connect to</param>
        /// <param name="expectedServer">The expected server UUID at the given location</param>
        /// <param name="details">Connection details provided by the server</param>
        /// <returns>A server data object</returns>
        ServerData Connect( string host, int port, Guid? expectedServer, IReadOnlyDictionary<string, string> details );
    }
}

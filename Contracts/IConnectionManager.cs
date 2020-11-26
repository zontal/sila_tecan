using System;
using System.Collections.Generic;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes the interface for a component that manages connections
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Refreshes the known servers
        /// </summary>
        /// <param name="timeout">Timeout to wait for responses from any server</param>
        void RefreshServerList( TimeSpan timeout );

        /// <summary>
        /// Gets a collection of currently known servers
        /// </summary>
        IEnumerable<ServerData> KnownServers { get; }

        /// <summary>
        /// Connects to a specific server
        /// </summary>
        /// <param name="serverUuid">The id of the server that should be connected to</param>
        /// <param name="timeout">The timeout or zero for no timeout</param>
        /// <returns>The connection to the specified server</returns>
        ServerData Connect( Guid serverUuid, TimeSpan timeout = default );
    }
}

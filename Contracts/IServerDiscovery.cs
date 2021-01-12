using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes the interface for a class responsible for server discovery
    /// </summary>
    public interface IServerDiscovery
    {
        /// <summary>
        /// Query a list of SiLA2 servers with valid SiLAService
        /// </summary>
        /// <param name="timeout">duration of Query period</param>
        /// <param name="networkFilter">A predicate that filters network interfaces to search for</param>
        /// <returns>"List of valid SiLA2 servers</returns>
        IEnumerable<ServerData> GetServers(TimeSpan timeout, Predicate<NetworkInterface> networkFilter = null );

        /// <summary>
        /// Connect to the server with the given unique identifier
        /// </summary>
        /// <param name="serverUuid">The unique identifier of the server</param>
        /// <param name="timeout">duration of Query period</param>
        /// <param name="networkFilter">A predicate that filters network interfaces to search for</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The connected server or null, if connection failed</returns>
        ServerData Connect( Guid serverUuid, TimeSpan timeout, Predicate<NetworkInterface> networkFilter = null, CancellationToken cancellationToken = default );

        /// <summary>
        /// Discovers server connections
        /// </summary>
        /// <param name="serverFoundCallback">Callback that should be executed when a new server is discovered</param>
        /// <param name="timeout">Maximum time for discovery</param>
        /// <param name="networkFilter">A predicate that filters network interfaces to search for</param>
        /// <param name="cancellationToken">A cancellation token that can be used to stop the discovery</param>
        void DiscoverServers( Action<ServerData> serverFoundCallback, TimeSpan timeout = default, Predicate<NetworkInterface> networkFilter = null, CancellationToken cancellationToken = default );
    }
}
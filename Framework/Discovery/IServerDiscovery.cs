using System;
using System.Collections.Generic;
using Grpc.Core;

namespace Tecan.Sila2.Discovery
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
        /// <returns>"List of valid SiLA2 servers</returns>
        IEnumerable<ServerData> GetServers(TimeSpan timeout);

        /// <summary>
        /// Load all server properties 
        /// </summary>
        ServerData GetServerData(Channel channel);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes a client that pools servers through server-initaietd connections
    /// </summary>
    public interface IServerPool
    {
        /// <summary>
        /// Gets a collection of currently connected servers
        /// </summary>
        IEnumerable<ServerData> ConnectedServers { get; }

        /// <summary>
        /// Gets fired when a new server gets connected
        /// </summary>
        event EventHandler<ServerDataEventArgs> ServerConnected;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes a connection to a server pool
    /// </summary>
    public interface IPoolConnection : IDisposable
    {
        /// <summary>
        /// The host that the server is connected to
        /// </summary>
        string Host { get; }

        /// <summary>
        /// True, if the connection is alive, false otherwise
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Attempts to restart the connection. If the connection is alive, this method does nothing. If the connection is not alive, it tries to reconnect to the server pool.
        /// </summary>
        void Restart();
    }
}

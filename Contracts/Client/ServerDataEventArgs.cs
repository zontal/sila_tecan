using System;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes the event data for an event that concerns a server
    /// </summary>
    public class ServerDataEventArgs : EventArgs
    {
        /// <summary>
        /// The server that the event is about
        /// </summary>
        public ServerData Server { get; }

        /// <summary>
        /// Initializes a new instance
        /// </summary>
        /// <param name="server">The server that the event is about</param>
        public ServerDataEventArgs(ServerData server)
        {
            Server = server;
        }
    }
}

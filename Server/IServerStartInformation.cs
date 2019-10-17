using System;
using System.Net.NetworkInformation;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes start information that a SiLA2 server needs to start
    /// </summary>
    public class ServerStartInformation
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="info">The server information</param>
        /// <param name="port">The port the server is running on</param>
        /// <param name="networkInterface">The network interface the server uses for discovery</param>
        /// <param name="serverUuid">The unique identifier of the server</param>
        public ServerStartInformation( ServerInformation info, int port, NetworkInterface networkInterface, Guid serverUuid )
        {
            Info = info;
            Port = port;
            NetworkInterface = networkInterface;
            ServerUuid = serverUuid;
        }

        /// <summary>
        /// Gets the server information
        /// </summary>
        public ServerInformation Info { get; }

        /// <summary>
        /// Gets the port the server is running on
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the network interface the server uses for discovery
        /// </summary>
        public NetworkInterface NetworkInterface { get; }

        /// <summary>
        /// Gets the unique identifier of the server
        /// </summary>
        public Guid ServerUuid { get; }
    }
}

using System;
using System.Collections.Generic;
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
        /// <param name="networkInterfaces">The network interfaces the server uses for discovery</param>
        /// <param name="serverUuid">The unique identifier of the server</param>
        /// <param name="name">the initial name of the server</param>
        public ServerStartInformation( ServerInformation info, int port, Predicate<NetworkInterface> networkInterfaces, Guid serverUuid, string name )
        {
            Name = name;
            Info = info;
            Port = port;
            NetworkInterfaces = networkInterfaces;
            ServerUuid = serverUuid;
        }

        /// <summary>
        /// Gets the server information
        /// </summary>
        public ServerInformation Info { get; }

        /// <summary>
        /// Gets the initial name of the server
        /// </summary>
        public string Name
        {
            get;
        }

        /// <summary>
        /// Gets the port the server is running on
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Filters the network interface the server uses for discovery
        /// </summary>
        public Predicate<NetworkInterface> NetworkInterfaces { get; }

        /// <summary>
        /// Gets the unique identifier of the server
        /// </summary>
        public Guid ServerUuid { get; }
    }
}

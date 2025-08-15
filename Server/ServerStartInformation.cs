using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes start information that a SiLA2 server needs to start
    /// </summary>
    public class ServerStartInformation
    {
        /// <summary>
        /// Denotes the default server configuration name
        /// </summary>
        public const string DefaultServerConfigurationName = "default";

        /// <summary>
        /// Gets the default port assignment
        /// </summary>
        public const int DefaultPort = 50052;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="info">The server information</param>
        /// <param name="port">The port the server is running on</param>
        public ServerStartInformation( ServerInformation info, int port )
            : this( info, port, null, default, null )
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="info">The server information</param>
        /// <param name="port">The port the server is running on</param>
        /// <param name="networkInterfaces">The network interfaces the server uses for discovery</param>
        /// <param name="serverUuid">The unique identifier of the server</param>
        /// <param name="name">the initial name of the server</param>
        public ServerStartInformation( ServerInformation info, int? port, string networkInterfaces, Guid serverUuid, string name )
            : this( info, port, networkInterfaces, serverUuid, name, null )
        {
        }


        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="info">The server information</param>
        /// <param name="port">The port the server is running on</param>
        /// <param name="networkInterfaces">The network interfaces the server uses for discovery</param>
        /// <param name="serverUuid">The unique identifier of the server</param>
        /// <param name="name">the initial name of the server</param>
        /// <param name="configName">The name of the configuration to use</param>
        public ServerStartInformation( ServerInformation info, int? port, string networkInterfaces, Guid serverUuid, string name, string configName )
        {
            Name = name;
            Info = info;
            Port = port;
            NetworkInterfaces = networkInterfaces;
            ServerUuid = serverUuid;
            ConfigName = configName ?? DefaultServerConfigurationName;
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
        /// Gets the name of the configuration set that should be used
        /// </summary>
        public string ConfigName
        {
            get;
        }

        /// <summary>
        /// Gets the port the server is running on
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Filters the network interface the server uses for discovery
        /// </summary>
        public string NetworkInterfaces { get; }

        /// <summary>
        /// Gets the unique identifier of the server
        /// </summary>
        public Guid ServerUuid { get; set; }

        /// <summary>
        /// Gets or sets the server certificate to use
        /// </summary>
        public X509Certificate2 ServerCertificate { 
            get;
            set; }
    }
}

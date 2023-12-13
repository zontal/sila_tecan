using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Tecan.Sila2.Client;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes data collected for a server
    /// </summary>
    public class ServerData
    {
        /// <summary>
        /// The configuration of the server
        /// </summary>
        public ServerConfig Config { get; }

        /// <summary>
        /// Denotes the channel to the server, if connected
        /// </summary>
        public IClientChannel Channel { get; }

        /// <summary>
        /// Name and implemented features of the server
        /// </summary>
        public ServerInformation Info { get; }

        /// <summary>
        /// A list of features implemented by the server
        /// </summary>
        public IEnumerable<Feature> Features { get; }

        /// <summary>
        /// Gets the certificate from the server
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Server model constructor (immutable object)
        /// </summary>
        /// <param name="serverConfig">Server configuration data</param>
        /// <param name="serverInformation">Server information</param>
        /// <param name="features">Server features</param>
        /// <param name="channel">The channel to the server</param>
        public ServerData(ServerConfig serverConfig, ServerInformation serverInformation, IEnumerable<Feature> features, IClientChannel channel)
            : this(serverConfig, serverInformation, features, channel, null)
        {
        }

        /// <summary>
        /// Server model constructor (immutable object)
        /// </summary>
        /// <param name="serverConfig">Server configuration data</param>
        /// <param name="serverInformation">Server information</param>
        /// <param name="features">Server features</param>
        /// <param name="channel">The channel to the server</param>
        /// <param name="certificate">the certificate from the server</param>
        public ServerData(ServerConfig serverConfig, ServerInformation serverInformation, IEnumerable<Feature> features, IClientChannel channel, X509Certificate2 certificate)
        {
            Config = serverConfig;
            Info = serverInformation;
            Features = features;
            Channel = channel;
            Certificate = certificate;
        }
    }
}
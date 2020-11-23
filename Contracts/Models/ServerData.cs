using System.Collections.Generic;

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
        /// Name and implemented features of the server
        /// </summary>
        public ServerInformation Info { get; }

        /// <summary>
        /// A list of features implemented by the server
        /// </summary>
        public IEnumerable<Feature> Features { get; }

        /// <summary>
        /// Server model constructor (immutable object)
        /// </summary>
        /// <param name="serverConfig">Server configuration data</param>
        /// <param name="serverInformation">Server information</param>
        /// <param name="features">Server features</param>
        public ServerData(ServerConfig serverConfig, ServerInformation serverInformation, IEnumerable<Feature> features)
        {
            Config = serverConfig;
            Info = serverInformation;
            Features = features;
        }
    }
}
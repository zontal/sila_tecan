using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes server information for servers that are connected with a channel
    /// </summary>
    public class GrpcServerData : ServerData
    {
        /// <summary>
        /// Server model constructor (immutable object)
        /// </summary>
        /// <param name="channel">The gRPC channel to the server</param>
        /// <param name="serverConfig">Server configuration data</param>
        /// <param name="serverInformation">Server information</param>
        /// <param name="features">Server features</param>
        public GrpcServerData(Channel channel, ServerConfig serverConfig, ServerInformation serverInformation, IEnumerable<Feature> features) : base(serverConfig, serverInformation, features)
        {
            Channel = channel;
        }

        /// <summary>
        /// Gets the underlying gRPC channel to this server
        /// </summary>
        public Channel Channel
        {
            get;
        }
    }
}

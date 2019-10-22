using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Grpc.Core;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Discovery
{
    /// <summary>
    /// Utility class to be used to discover and find SiLA Servers using the 
    /// ServiceFinder class.
    /// </summary>
    public class ServerDiscovery : IServerDiscovery
    {
        private readonly ILoggingChannel _loggingChannel;
        private readonly ServiceFinder _serviceFinder;
        private readonly IClientExecutionManager _executionManager;

        /// <summary>
        /// Create a new sila discovery object
        /// </summary>
        /// <param name="factory">A logging factory</param>
        /// <param name="executionManager">The client execution manager</param>
        public ServerDiscovery(ILoggingChannelFactory factory, IClientExecutionManager executionManager)
        {
            _loggingChannel = factory.CreateChannel(typeof(ServerDiscovery));
            _serviceFinder = new ServiceFinder(factory, new Networking(factory));
            _executionManager = executionManager;
        }
        
        /// <summary>
        /// Query a list of SiLA2 servers with valid SiLAService
        /// </summary>
        /// <param name="timeout">duration of Query period</param>
        /// <returns>"List of valid SiLA2 servers</returns>
        public IEnumerable<ServerData> GetServers(TimeSpan timeout)
        {
            return _serviceFinder.QueryAllServices(timeout)
                .Select(channel =>
                {
                    try
                    {
                        return GetServerData(channel);
                    }
                    catch (Exception e)
                    {
                        _loggingChannel.LogException($"Invalid server found at channel: {channel.Target}".ToString, e);
                        return null;
                    }
                })
                .Where(data => data != null).ToList();
        }
        
        /// <summary>
        /// Load all server properties 
        /// </summary>
        public ServerData GetServerData(Channel channel)
        {
            var silaService = new SiLAServiceProvider.Client(channel, _executionManager);
            var config = GetServerConfig(silaService);
            var info = GetServerInfo(silaService);
            var features = GetImplementedFeatures(silaService);
            var server = new ServerData(channel, config, info, features);
            return server;
        }
        
        private static List<Feature> GetImplementedFeatures(SiLAServiceProvider.Client silaService)
        {
            return silaService.ImplementedFeatures.Select(f => FeatureSerializer.LoadFromXml(silaService.GetFeatureDefinition(f).Value)).ToList();
        }

        private static ServerConfig GetServerConfig(SiLAServiceProvider.Client silaService)
        {
            var name = silaService.ServerName;
            var uuid = Guid.Parse(silaService.ServerUUID);
            var config = new ServerConfig(name, uuid);
            return config;
        }

        private static ServerInformation GetServerInfo(SiLAServiceProvider.Client silaService)
        {
            var type = silaService.ServerType;
            var description = silaService.ServerDescription;
            var vendorUri = silaService.ServerVendorURL.Value;
            var version = silaService.ServerVersion;
            var info = new ServerInformation(type, description, vendorUri, version);
            return info;
        }
    }
}

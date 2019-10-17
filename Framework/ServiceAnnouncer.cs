using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Makaretu.Dns;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2
{
    /// <summary>
    /// Helper type for announcing the SiLA service via mDNS.
    /// </summary>
    public class ServiceAnnouncer : IDisposable
    {
        private readonly ILoggingChannel _logger;
        private readonly Networking _networking;
        private MulticastService _multicastService;
        private ServiceDiscovery _serviceDiscoverer;

        /// <summary>
        /// The service profile that is announced.
        /// </summary>
        public ServiceProfile Profile { get; private set; }

        /// <summary>
        /// The IP addresses and ports at which the service is being advertised.
        /// </summary>
        public IEnumerable<Tuple<IPAddress, int>> Endpoints
        {
            get
            {
                var addresses = new List<IPAddress>();
                if (Profile != null)
                {
                    int port = Profile.Resources.OfType<SRVRecord>().FirstOrDefault().Port;
                    addresses.AddRange(Profile.Resources.OfType<ARecord>().Select(r => r.Address));
                    addresses.AddRange(Profile.Resources.OfType<AAAARecord>().Select(r => r.Address));
                    return addresses.Select(a => new Tuple<IPAddress, int>(a, port));
                }
                return null;
            }
        }

        /// <summary>
        /// Creates a helper object that can announce as a SiLA service on mDNS as long as it exists.
        /// </summary>
        /// <param name="instanceName">Unique name of the SiLA device service</param>
        /// <param name="portNumber">Listening port of the gRPC server</param>
        /// <param name="networkInterface">The network interface to advertise on</param>
        /// <param name="loggingChannelFactory">A factory to create logging channels</param>
        public ServiceAnnouncer( string instanceName, ushort portNumber, NetworkInterface networkInterface, ILoggingChannelFactory loggingChannelFactory )
            : this(instanceName, portNumber, networkInterface, loggingChannelFactory, new Networking(loggingChannelFactory))
        {
        }

        /// <summary>
        /// Creates a helper object that can announce as a SiLA service on mDNS as long as it exists.
        /// </summary>
        /// <param name="instanceName">Unique name of the SiLA device service</param>
        /// <param name="portNumber">Listening port of the gRPC server</param>
        /// <param name="networkInterface">The network interface to advertise on</param>
        /// <param name="loggingChannelFactory">A factory to create logging channels</param>
        /// <param name="networking">A networking library</param>
        public ServiceAnnouncer( string instanceName, ushort portNumber, NetworkInterface networkInterface, ILoggingChannelFactory loggingChannelFactory, Networking networking )
        {
            _logger = loggingChannelFactory.CreateChannel( typeof(ServiceAnnouncer) );
            _networking = networking;

            var selectedAddresses = _networking.ListInterNetworkAddresses( networkInterface );
            var filterFunc = _networking.CreateFilterFunc( networkInterface );

            Profile = new ServiceProfile( instanceName, DiscoveryConstants.ServiceName, portNumber, selectedAddresses );
            _multicastService = new MulticastService( filterFunc );
        }

        /// <summary>
        /// Starts advertising the SiLA server on mDNS.
        /// </summary>
        public void Start()
        {
            _serviceDiscoverer?.Dispose();
            _logger.Log("Starting multicast service".ToString, Severity.Info);
            _multicastService.Start();
            _serviceDiscoverer = new ServiceDiscovery(_multicastService);
            _logger.Log("Starting to advertise".ToString, Severity.Info);
            _serviceDiscoverer.Advertise(Profile);
        }

        /// <summary>
        /// Stops advertising and releases resources.
        /// </summary>
        public void Dispose()
        {
            _serviceDiscoverer?.Dispose();
            _serviceDiscoverer = null;
            _multicastService?.Dispose();
            _multicastService = null;
        }
    }

}

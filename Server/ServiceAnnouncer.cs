using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Common.Logging;
using Makaretu.Dns;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2
{
    /// <summary>
    /// Helper type for announcing the SiLA service via mDNS.
    /// </summary>
    public class ServiceAnnouncer : IDisposable
    {
        private readonly ILog _logger = LogManager.GetLogger<ServiceAnnouncer>();
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
                if(Profile != null)
                {
                    int port = Profile.Resources.OfType<SRVRecord>().FirstOrDefault().Port;
                    addresses.AddRange( Profile.Resources.OfType<ARecord>().Select( r => r.Address ) );
                    addresses.AddRange( Profile.Resources.OfType<AAAARecord>().Select( r => r.Address ) );
                    return addresses.Select( a => new Tuple<IPAddress, int>( a, port ) );
                }
                return null;
            }
        }

        /// <summary>
        /// Creates a helper object that can announce as a SiLA service on mDNS as long as it exists.
        /// </summary>
        /// <param name="instanceName">Unique name of the SiLA device service</param>
        /// <param name="portNumber">Listening port of the gRPC server</param>
        /// <param name="networkFilter">A predicate that filters network interfaces to advertise on</param>
        public ServiceAnnouncer( string instanceName, ushort portNumber, Predicate<NetworkInterface> networkFilter )
        {
            var selectedAddresses = Networking.ListInterNetworkAddresses( networkFilter );
            Profile = new ServiceProfile( instanceName, DiscoveryConstants.ServiceName, portNumber, selectedAddresses );
            _multicastService = new MulticastService( Networking.CreateFilter( networkFilter ) );
        }

        /// <summary>
        /// Adds a property to the announced profile
        /// </summary>
        /// <param name="key">The name of the property</param>
        /// <param name="value">The value of the property</param>
        public void SetProperty( string key, string value )
        {
            Profile.AddProperty( key, value );
        }

        /// <summary>
        /// Starts advertising the SiLA server on mDNS.
        /// </summary>
        public void Start()
        {
            _serviceDiscoverer?.Dispose();
            _logger.Info( "Starting multicast service" );
            _multicastService.Start();
            _serviceDiscoverer = new ServiceDiscovery( _multicastService );
            _logger.Info( "Starting to advertise" );
            _serviceDiscoverer.Advertise( Profile );
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

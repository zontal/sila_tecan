using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Tecan.Sila2
{
    /// <summary>
    /// Networking utility library listing network interfaces and finding 
    /// interfaces based on name or CIDR
    /// </summary>
    public class Networking
    {
        private static readonly ILog _loggingChannel = LogManager.GetLogger<Networking>();

        /// <summary>
        /// Enumerates the InterNetwork Unicast addresses for a given network interface.
        /// </summary>
        /// <param name="networkInterfaceFilter">A filter of network interfaces to inspect</param>
        /// <returns>list of InterNetwork Unicast IP v4 addresses</returns>
        public static IEnumerable<IPAddress> ListInterNetworkAddresses(Predicate<NetworkInterface> networkInterfaceFilter)
        {
            return ListInterNetworkAddresses(networkInterfaceFilter, true);
        }

        /// <summary>
        /// Enumerates the InterNetwork Unicast addresses for a given network interface.
        /// </summary>
        /// <param name="networkInterfaceFilter">A filter of network interfaces to inspect</param>
        /// <param name="requireMulticast">True, if only multicast network addresses are to be used, otherwise False</param>
        /// <returns>list of InterNetwork Unicast IP v4 addresses</returns>
        public static IEnumerable<IPAddress> ListInterNetworkAddresses( Predicate<NetworkInterface> networkInterfaceFilter, bool requireMulticast )
        {
            IEnumerable<NetworkInterface> networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            if (requireMulticast)
            {
                networkInterfaces = networkInterfaces.Where(nic => nic.SupportsMulticast);
            }
            if(networkInterfaceFilter != null)
            {
                networkInterfaces = networkInterfaces.Where( nic => networkInterfaceFilter( nic ) );
            }
            var unicastAddresses = networkInterfaces
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses.Select(u => u.Address))
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork);
            return unicastAddresses;
        }



        /// <summary>
        /// Creates a network interface filter
        /// </summary>
        /// <param name="filterString">The filter string</param>
        /// <returns>A predicate for network interfaces</returns>
        public static Predicate<NetworkInterface> CreateNetworkInterfaceFilter( string filterString )
        {
            if(string.IsNullOrEmpty( filterString ) || filterString == "*")
            {
                return nic => nic.OperationalStatus == OperationalStatus.Up;
            }

            var parts = new List<NetworkInterfaceFilter>();
            foreach(var part in filterString.Split( ',' ))
            {
                var trimmedPart = part.Trim();
                if(IPAddress.TryParse( trimmedPart, out var address ))
                {
                    parts.Add( new IpFilter( address ) );
                }
                else if(IPNetwork.TryParse( trimmedPart, out var network ))
                {
                    parts.Add( new IpnetFilter( network ) );
                }
                else
                {
                    var regex = new Regex( "\\A" + trimmedPart + "\\Z" );
                    parts.Add( new RegexFilter( regex ) );
                }
            }

            return nic => parts.Any( pred => pred.PassFilter( nic ) ) && CheckOperationalStatus( nic );
        }

        private static bool CheckOperationalStatus( NetworkInterface nic )
        {
            if(nic.OperationalStatus != OperationalStatus.Up)
            {
                _loggingChannel.Warn( $"Network {nic.Name} matches the filter criteria but is {nic.OperationalStatus}." );
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create a network interface filter function
        /// </summary>
        /// <param name="networkInterfaceFilter">The inner network interface filter function</param>
        /// <returns>A network interface filter function</returns>
        public static Func<IEnumerable<NetworkInterface>, IEnumerable<NetworkInterface>> CreateFilter( Predicate<NetworkInterface> networkInterfaceFilter )
        {
            if(networkInterfaceFilter == null)
            {
                return null;
            }

            IEnumerable<NetworkInterface> filter( IEnumerable<NetworkInterface> nics )
            {
                foreach(var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if(networkInterfaceFilter( nic ))
                    {
                        if(CheckOperationalStatus( nic ))
                        {
                            _loggingChannel.Info( $"Advertising service on network interface {nic.Name}" );
                            yield return nic;
                        }
                    }
                    else
                    {
                        _loggingChannel.Debug( $"Skipping network interface {nic.Name}" );
                    }
                }
            }

            return filter;
        }

        private abstract class NetworkInterfaceFilter
        {
            public abstract bool PassFilter( NetworkInterface nic );
        }

        private class IpFilter : NetworkInterfaceFilter
        {
            private IPAddress _filterAddress;

            public IpFilter( IPAddress filterAddress )
            {
                _filterAddress = filterAddress;
            }

            public override bool PassFilter( NetworkInterface nic )
            {
                return nic.GetIPProperties().UnicastAddresses.Any( ua => ua.Address.Equals( _filterAddress ) );
            }
        }

        private class IpnetFilter : NetworkInterfaceFilter
        {
            private IPNetwork _filterNetwork;

            public IpnetFilter( IPNetwork filterNetwork )
            {
                _filterNetwork = filterNetwork;
            }

            public override bool PassFilter( NetworkInterface nic )
            {
                return nic.GetIPProperties().UnicastAddresses.Any( ua => _filterNetwork.Contains( ua.Address ) );
            }
        }

        private class RegexFilter : NetworkInterfaceFilter
        {
            private Regex _filterRegex;

            public RegexFilter( Regex regex )
            {
                _filterRegex = regex;
            }

            public override bool PassFilter( NetworkInterface nic )
            {
                return _filterRegex.IsMatch( nic.Name );
            }
        }
    }
}

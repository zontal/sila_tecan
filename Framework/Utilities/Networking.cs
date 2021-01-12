using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Tecan.MAP.Contracts.Services.Logging;

namespace Tecan.Sila2
{
    /// <summary>
    /// Networking utility library listing network interfaces and finding 
    /// interfaces based on name or CIDR
    /// </summary>
    public static class Networking
    {

        /// <summary>
        /// Finds the network interface that has a matching interface name
        /// </summary>
        /// <param name="interfaceName">interface name to find</param>
        /// <returns>the matching found network interface</returns>
        public static NetworkInterface GetNetworkInterfaceByName(string interfaceName)
        {
            if(interfaceName == null)
            {
                throw new NetworkInterfaceNotFoundException("No network interface name was specified");
            }

            return NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault( nic => nic.Name.Contains( interfaceName ) );
        }

        /// <summary>
        /// Finds the first network interface that has a matching unicast address-
        /// </summary>
        /// <param name="mask">IPv4/IPv6 address or CIDR mask.</param>
        /// <returns>a matching interface</returns>
        public static NetworkInterface GetNetworkInterfaceByCidr(string mask)
        {
            if(mask.Contains('/'))
            {
                IPNetwork ipnetwork = IPNetwork.Parse(mask);
                return NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault( nic => nic.GetIPProperties().UnicastAddresses.Any( ua => ipnetwork.Contains( ua.Address ) ) );

            }
            else
            {
                IPAddress address = IPAddress.Parse(mask);

                return NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault( nic => nic.GetIPProperties().UnicastAddresses.Any(ua => ua.Address == address) );
            }
        }

        /// <summary>
        /// Enumerates the InterNetwork Unicast addresses for a given network interface.
        /// </summary>
        /// <param name="networkInterfaceFilter">A filter of network interfaces to inspect</param>
        /// <returns>list of InterNetwork Unicast IP v4 addresses</returns>
        public static IEnumerable<IPAddress> ListInterNetworkAddresses(Predicate<NetworkInterface> networkInterfaceFilter)
        {
            IEnumerable<NetworkInterface> networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            if (networkInterfaceFilter != null)
            {
                networkInterfaces = networkInterfaces.Where( nic => networkInterfaceFilter(nic) );
            }
            var unicastAddresses = networkInterfaces.SelectMany(nic => nic.GetIPProperties().UnicastAddresses.Select(u => u.Address));
            return unicastAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}

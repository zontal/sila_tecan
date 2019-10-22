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
    internal class Networking
    {
        private readonly ILoggingChannel Log;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="loggingChannelFactory">The logging factory</param>
        public Networking(ILoggingChannelFactory loggingChannelFactory)
        {
            Log = loggingChannelFactory.CreateChannel(typeof(Networking));
        }

        /// <summary>
        /// Finds the network interface that has a matching interface name or matching unicast address
        /// </summary>
        /// <param name="interfaceNameOrCidr">interface name to find or the IPv4/IPv6 address or CIDR mask. </param>
        /// <returns>the matching found network interface</returns>
        public NetworkInterface GetNetworkInterface(string interfaceNameOrCidr)
        {    
            try
            {
                return GetNetworkInterfaceByCidr(interfaceNameOrCidr);
            }
            catch(NetworkInterfaceNotFoundException e)
            {
                Log.Log(() => $"Could not get network interface by IP address or CIDR: {e.Message}", Severity.Warning);
            }
            catch(FormatException)
            {
                Log.Log(() => $"{interfaceNameOrCidr} is not an IP address or CIDR mask", Severity.Warning);
            }
            
            return GetNetworkInterfaceByName(interfaceNameOrCidr);
        }

        /// <summary>
        /// Finds the network interface that has a matching interface name
        /// </summary>
        /// <param name="interfaceName">interface name to find</param>
        /// <returns>the matching found network interface</returns>
        public NetworkInterface GetNetworkInterfaceByName(string interfaceName)
        {
            if(interfaceName == null)
            {
                throw new NetworkInterfaceNotFoundException("No network interface name was specified");
            }

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if(nic.Name.Contains(interfaceName))
                {
                    if(nic.OperationalStatus != OperationalStatus.Up)
                    {
                        Log.Log(() => $"Interface {nic.Name} is not Up!", Severity.Warning);
                    }
                    Log.Log(() => $"Interface {nic.Name} found!", Severity.Info);
                    return nic;
                }
            }
            throw new NetworkInterfaceNotFoundException($"Interface {interfaceName} not found for Server Discovery");
        }

        /// <summary>
        /// Finds the first network interface that has a matching unicast address-
        /// </summary>
        /// <param name="mask">IPv4/IPv6 address or CIDR mask.</param>
        /// <returns>a matching interface</returns>
        public NetworkInterface GetNetworkInterfaceByCidr(string mask)
        {
            if(mask.Contains('/'))
            {
                IPNetwork ipnetwork = IPNetwork.Parse(mask);
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var unicastAddresses = nic.GetIPProperties().UnicastAddresses;
                    if (!unicastAddresses.Any(ua => ipnetwork.Contains(ua.Address))) continue;
                    Log.Log(() => $"Interface {nic.Name} found!", Severity.Info);
                    return nic;
                }
            }
            else
            {
                IPAddress address = IPAddress.Parse(mask);
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var unicastAddresses = nic.GetIPProperties().UnicastAddresses;
                    if(unicastAddresses.Select(ua => ua.Address).Contains(address))
                    {
                        Log.Log(() => $"Interface {nic.Name} found!", Severity.Info);
                        return nic;
                    }
                }
            }
            throw new NetworkInterfaceNotFoundException($"No network interface found for IP address/CIDR Mask {mask}");
        }

        /// <summary>
        /// Finds a valid network interface on the system which is NOT loopback and is up
        /// </summary>
        /// <returns>a valid network interface</returns>
        public NetworkInterface FindValidNetworkInterface()
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                Log.Log(() => $"Interface {nic.Name} found!", Severity.Info);
                return nic;
            }
            throw new NetworkInterfaceNotFoundException($"No network interface was found that was UP and not Loopback");
        }

        /// <summary>
        /// Creates a filtering func that can be passed to the Makaretu MulticastService.
        /// </summary>
        /// <param name="selectedInterfaces">Whitelist of network interfaces</param>
        /// <returns>
        /// A func that takes a list of interfaces and returns a list of those that are whitelisted.
        /// If no interfaces are passed to <para>selectedInterfaces</para>, no filter is applied.
        /// </returns>
        public Func<IEnumerable<NetworkInterface>, IEnumerable<NetworkInterface>> CreateFilterFunc(params NetworkInterface[] selectedInterfaces)
        {
            IEnumerable<NetworkInterface> FilterFunc(IEnumerable<NetworkInterface> nics)
            {
                if (selectedInterfaces.Length > 0)
                {
                    var filtered = nics.Where(ni => selectedInterfaces.Select(n => n.Id).Contains(ni.Id));
                    Log.Log($"Filtered {nics.Count()} interfaces down to {filtered.Count()} using a whitelist of {selectedInterfaces.Length}".ToString, Severity.Debug);
                    return filtered;
                }
                return nics;
            }
            return FilterFunc;
        }

        /// <summary>
        /// Enumerates the InterNetwork Unicast addresses for a given network interface.
        /// </summary>
        /// <param name="networkInterface">A network interface to inspect</param>
        /// <returns>list of InterNetwork Unicast IP v4 addresses</returns>
        public IEnumerable<IPAddress> ListInterNetworkAddresses(NetworkInterface networkInterface)
        {
            var unicastAddresses = networkInterface.GetIPProperties().UnicastAddresses.Select(u => u.Address);
            return unicastAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}

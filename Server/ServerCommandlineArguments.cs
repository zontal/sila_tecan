using System;
using CommandLine;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes the command line parameters for starting a SiLA2 Server
    /// </summary>
    public class ServerCommandLineArguments
    {
        /// <summary>
        /// Port to connect server to.
        /// </summary>
        [Option('p', "port", Default = 50052, HelpText = "Port to connect server to.")]
        public int Port { get; set; }

        /// <summary>
        /// Network interface/IP Address/ Mask to use for discovery (e.g. lo, wlp1s0, eth0, 192.168.1.0/24, 192.168.1.0)
        /// </summary>
        [Option('i', "interface", Required = false,
            HelpText = "Network interface/IP Address/ Mask to use for discovery (e.g. lo, wlp1s0, eth0, 192.168.1.0/24, 192.168.1.0)")]
        public string InterfaceNameOrCIDR { get; set; }

        /// <summary>
        /// The server UUID to be used
        /// </summary>
        [Option('u', "uuid", HelpText = "The server UUID to be used ", Required = false)]
        public Guid ServerUuid { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The initial name of the server. If omitted, this defaults to the server type.
        /// </summary>
        [Option('n', "name", Required = false, HelpText = "The initial name of the server. If omitted, this defaults to the server type.")]
        public string Name
        {
            get;
            set;
        }
    }
}

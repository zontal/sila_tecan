using System;
using System.Collections.Generic;
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
        [Option('p', "port", HelpText = "Port to connect server to.")]
        public int? Port { get; set; }

        /// <summary>
        /// Network interface/IP Address/ Mask filter to use for discovery (e.g. lo, wlp1s0, eth\d, 192.168.1.0/24, 192.168.1.0), can be regex
        /// </summary>
        [Option( 'i', "interface", Required = false,
            HelpText = "Network interface/IP Address/ Mask filter to use for discovery (e.g. lo, wlp1s0, eth\\d, 192.168.1.0/24, 192.168.1.0), can be regex" )]
        public string InterfaceFilter { get; set; } = "*";

        /// <summary>
        /// If set, the server will not be discoverable on the network.
        /// </summary>
        [Option("no-discovery", Required = false, HelpText = "If set, the server will not be discoverable on the network.")]
        public bool NoDiscovery { get; set; } = false;

        /// <summary>
        /// The server UUID to be used
        /// </summary>
        [Option( 'u', "uuid", HelpText = "The server UUID to be used ", Required = false )]
        public Guid ServerUuid { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the name of the configuration to use. If omitted, this defaults to "default".
        /// </summary>
        [Option("config-name", HelpText = "The name of the configuration to use. If omitted, this defaults to \"default\".", Required = false)]
        public string ConfigName { get; set; }

        /// <summary>
        /// Gets or sets the path to the server certificate
        /// </summary>
        [Option('c', "certificate", HelpText = "The path to the server certíficate", Required = false)]
        public string CertificatePath { get; set; }

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

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
        // The help texts are shared with ServerCommandLineParser, which prints the help screen without reflection.
        internal const string PortHelpText = "Port to connect server to.";
        internal const string InterfaceFilterHelpText = "Network interface/IP Address/ Mask filter to use for discovery (e.g. lo, wlp1s0, eth\\d, 192.168.1.0/24, 192.168.1.0), can be regex";
        internal const string NoDiscoveryHelpText = "If set, the server will not be discoverable on the network.";
        internal const string ServerUuidHelpText = "The server UUID to be used ";
        internal const string ConfigNameHelpText = "The name of the configuration to use. If omitted, this defaults to \"default\".";
        internal const string CertificatePathHelpText = "The path to the server certíficate";
        internal const string NameHelpText = "The initial name of the server. If omitted, this defaults to the server type.";

        /// <summary>
        /// Port to connect server to.
        /// </summary>
        [Option('p', "port", HelpText = PortHelpText)]
        public int? Port { get; set; }

        /// <summary>
        /// Network interface/IP Address/ Mask filter to use for discovery (e.g. lo, wlp1s0, eth\d, 192.168.1.0/24, 192.168.1.0), can be regex
        /// </summary>
        [Option( 'i', "interface", Required = false,
            HelpText = InterfaceFilterHelpText )]
        public string InterfaceFilter { get; set; } = "*";

        /// <summary>
        /// If set, the server will not be discoverable on the network.
        /// </summary>
        [Option("no-discovery", Required = false, HelpText = NoDiscoveryHelpText)]
        public bool NoDiscovery { get; set; } = false;

        /// <summary>
        /// The server UUID to be used
        /// </summary>
        [Option( 'u', "uuid", HelpText = ServerUuidHelpText, Required = false )]
        public Guid ServerUuid { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the name of the configuration to use. If omitted, this defaults to "default".
        /// </summary>
        [Option("config-name", HelpText = ConfigNameHelpText, Required = false)]
        public string ConfigName { get; set; }

        /// <summary>
        /// Gets or sets the path to the server certificate
        /// </summary>
        [Option('c', "certificate", HelpText = CertificatePathHelpText, Required = false)]
        public string CertificatePath { get; set; }

        /// <summary>
        /// The initial name of the server. If omitted, this defaults to the server type.
        /// </summary>
        [Option('n', "name", Required = false, HelpText = NameHelpText)]
        public string Name
        {
            get;
            set;
        }
    }
}

using System;
using CommandLine;

namespace Tecan.Sila2.Server
{
    public class ServerCommandLineArguments
    {
        [Option('p', "port", Default = 50052, HelpText = "Port to connect server to.")]
        public int Port { get; set; }

        [Option('i', "interface", Required = false,
            HelpText = "Network interface/IP Address/ Mask to use for discovery (e.g. lo, wlp1s0, eth0, 192.168.1.0/24, 192.168.1.0)")]
        public string InterfaceNameOrCIDR { get; set; }

        [Option('u', "uuid", HelpText = "The server UUID to be used ", Required = false)]
        public Guid ServerUuid { get; set; } = Guid.NewGuid();
    }
}

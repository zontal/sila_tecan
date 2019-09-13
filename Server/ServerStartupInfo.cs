using System;
using System.Net.NetworkInformation;

namespace Tecan.Sila2.Server
{
    internal class ServerStartupInfo : IServerStartInformation
    {
        public ServerStartupInfo(ServerInformation info, int port, NetworkInterface networkInterface, Guid serverUuid)
        {
            Info = info;
            Port = port;
            NetworkInterface = networkInterface;
            ServerUuid = serverUuid;
        }

        public ServerInformation Info { get; }

        public int Port { get; }

        public NetworkInterface NetworkInterface { get; }

        public Guid ServerUuid { get; }
    }
}

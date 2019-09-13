using System;
using System.Net.NetworkInformation;

namespace Tecan.Sila2.Server
{
    public interface IServerStartInformation
    {
        ServerInformation Info { get; }

        int Port { get; }

        NetworkInterface NetworkInterface { get; }

        Guid ServerUuid { get; }
    }
}

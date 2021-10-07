using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.IntegrationTests
{
    // Integration tests do not work on CI server, unfortunately
    [TestFixture( Category = "IntegrationTests" )]
    public class DiscoveryIntegrationTestFixture : IntegrationTestFixtureBase
    {
        protected override ServerData FindServer(Guid serverUuid)
        {
            var connector = new ServerConnector( new DiscoveryExecutionManager() );
            var discovery = new ServerDiscovery( connector );
            return discovery.Connect( serverUuid, TimeSpan.FromSeconds( 5 ), AcceptNetworkInterface );
        }

        private bool AcceptNetworkInterface( NetworkInterface nic )
        {
            return nic.NetworkInterfaceType == NetworkInterfaceType.Loopback;
        }
    }
}

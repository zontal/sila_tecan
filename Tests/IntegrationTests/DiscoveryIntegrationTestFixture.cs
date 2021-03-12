using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Tests;

namespace Tecan.Sila2.IntegrationTests
{
    [TestFixture]
    public class DiscoveryIntegrationTestFixture : IntegrationTestFixtureBase
    {
        protected override ServerData FindServer(Guid serverUuid)
        {
            var testLogging = new TestLoggingFactory();
            var connector = new ServerConnector( new DiscoveryExecutionManager(), testLogging );
            var discovery = new ServerDiscovery( testLogging, connector );
            return discovery.Connect( serverUuid, TimeSpan.FromSeconds( 5 ), AcceptNetworkInterface );
        }

        private bool AcceptNetworkInterface( NetworkInterface nic )
        {
            return nic.NetworkInterfaceType == NetworkInterfaceType.Loopback;
        }
    }
}

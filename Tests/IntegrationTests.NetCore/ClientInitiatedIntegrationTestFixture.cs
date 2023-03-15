using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.IntegrationTests
{
    // Integration tests do not work on CI server, unfortunately
    [TestFixture( Category = "IntegrationTests" )]
    public class ClientInitiatedIntegrationTestFixture : IntegrationTestFixtureBase
    {
        protected override ProcessStartInfo GetTestServerStartInfo( string payloadArguments )
        {
            var dir = FindServerDirectory( "TestServerManaged" );
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = "dotnet",
#if NET6_0
                Arguments = Path.Combine( dir, "net6.0", "TestServer.NetCore.dll" ) + " " + payloadArguments,
#else
                Arguments = Path.Combine( dir, "net7.0", "TestServer.NetCore.dll" ) + " " + payloadArguments,
#endif
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false,
                WorkingDirectory = dir
            };
            return processStartInfo;
        }

        protected override ServerData FindServer( Guid serverUuid )
        {
            var connector = new ServerConnector( new DiscoveryExecutionManager() );

            return connector.Connect( IPAddress.Loopback, 50052, null, new Dictionary<string, string>
            {
                {"encrypted", "false" }
            } );
        }
    }

    [TestFixture( Category = "IntegrationTests" )]
    public class ClientInitiatedIntegrationTestAgainstLegacyGrpcFixture : ClientInitiatedIntegrationTestFixture
    {
        protected override ProcessStartInfo GetTestServerStartInfo( string payloadArguments )
        {
            var dir = FindServerDirectory( "TestServer" );
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine( dir, "net48", "TestServer.exe" ),
                Arguments = payloadArguments,
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false,
                WorkingDirectory = dir
            };
            return processStartInfo;
        }
    }
}

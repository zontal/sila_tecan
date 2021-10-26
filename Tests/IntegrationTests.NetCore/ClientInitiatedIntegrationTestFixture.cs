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
            AppContext.SetSwitch( "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true );
            var dir = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "..", "TestServerManaged" );
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = "dotnet",
                Arguments = Path.Combine( dir, "netcoreapp3.1", "TestServer.NetCore.dll" ) + " " + payloadArguments,
                CreateNoWindow = true,
                ErrorDialog = false,
                RedirectStandardInput = true,
                RedirectStandardError = false,
                RedirectStandardOutput = false,
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
            AppContext.SetSwitch( "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true );
            var dir = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "..", "TestServer" );
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(dir, "net472", "TestServer.exe"),
                Arguments = payloadArguments,
                CreateNoWindow = true,
                ErrorDialog = false,
                RedirectStandardInput = true,
                RedirectStandardError = false,
                RedirectStandardOutput = false,
                UseShellExecute = false,
                WorkingDirectory = dir
            };
            return processStartInfo;
        }
    }
}

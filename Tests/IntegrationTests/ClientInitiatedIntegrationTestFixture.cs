using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace Tecan.Sila2.IntegrationTests
{
    // Integration tests do not work on CI server, unfortunately
    [TestFixture( Category = "IntegrationTests" )]
    public class ClientInitiatedIntegrationTestFixture : IntegrationTestFixtureBase
    {
    }

    [TestFixture( Category = "IntegrationTests")]
    public class ClientInitiatedIntegrationTestAgainstManagedGrpcFixture : ClientInitiatedIntegrationTestFixture
    {
        protected override ProcessStartInfo GetTestServerStartInfo( string payloadArguments )
        {
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
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NUnit.Framework;
using Tecan.Sila2.Authentication;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Locking;
using Tecan.Sila2.Server;
using Tecan.Sila2.Tests;

namespace Tecan.Sila2.IntegrationTests
{
    [TestFixture]
    public class IntegrationTestFixture
    {
        private ServerData _server;
        private Process _serverProcess;

        [SetUp]
        public void StartTestServer()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "TestServer");
            var serverId = Guid.NewGuid();
            var payloadArguments = $"-u {serverId}";
            var processStartInfo = new ProcessStartInfo()
            {
#if NETCOREAPP
                FileName = "dotnet",
                Arguments = Path.Combine(dir, "netcoreapp2.1", "TestServer.dll") + " " + payloadArguments,
#elif NET461
                FileName = Path.Combine(dir, "net461", "TestServer.exe"),
                Arguments = payloadArguments,
#elif NET472
                FileName = Path.Combine(dir, "net472", "TestServer.exe"),
                Arguments = payloadArguments,
#endif
                CreateNoWindow = true,
                ErrorDialog = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            _serverProcess = Process.Start(processStartInfo);
            StartTransferStream(_serverProcess.StandardError, Console.Error, () => _serverProcess.HasExited);
            StartTransferStream( _serverProcess.StandardOutput, Console.Out, () => _serverProcess.HasExited );

            Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
            var testLogging = new TestLoggingFactory();
            var serverDiscovery = new ServerDiscovery( testLogging, new DiscoveryExecutionManager() );
            var servers = serverDiscovery.GetServers( TimeSpan.FromSeconds( 1 ) );

            Assert.That( servers, Is.Not.Empty );
            _server = servers.FirstOrDefault( s => s.Config.Uuid == serverId );
            Assert.That( _server, Is.Not.Null );
        }

        private async void StartTransferStream(StreamReader source, TextWriter target, Func<bool> endCondition)
        {
            while( !endCondition() )
            {
                var line = await source.ReadLineAsync();
                await target.WriteLineAsync( line );
            }
        }

        [TearDown]
        public void StopTestServer()
        {
            if( _serverProcess != null && !_serverProcess.HasExited )
            {
                _serverProcess.StandardInput.WriteLine();
                _serverProcess.StandardInput.Flush();
                _serverProcess.WaitForExit( 1000 );
                if( !_serverProcess.HasExited )
                {
                    _serverProcess.Kill();
                }
            }
        }

        //[Test]
        public void PassBoolean_TransmittedSuccessfully()
        {
            var token = Login();
            var lockId = Lock( token );
            var executionManager = new TestAuthLockingExecutionManager( lockId, token, _server.Channel );
            var testClient = new TestServerProvider.Client(_server.Channel, executionManager);
            
            Assert.That(testClient.PassBool(true), Is.True);
            Assert.That(testClient.PassBool(false), Is.False);

            Unlock(lockId, token);
            Logout(token);
        }

        private string Login()
        {
            var authenticationClient = new AuthenticationServiceProvider.Client(_server.Channel, new DiscoveryExecutionManager());
            var token = authenticationClient.Login( "admin", "admin", _server.Config.Uuid.ToString(), new List<string>() );
            return token.AccessToken;
        }

        private string Lock( string token )
        {
            var lockingClient = new LockControllerProvider.Client(_server.Channel, new TestAuthLockingExecutionManager(null, token, _server.Channel));
            var lockIdentifier = Guid.NewGuid().ToString();
            lockingClient.LockServer(lockIdentifier, 3600);
            return lockIdentifier;
        }

        private void Unlock( string identifier, string token )
        {
            var lockingClient = new LockControllerProvider.Client( _server.Channel, new TestAuthLockingExecutionManager( null, token, _server.Channel ) );
            lockingClient.UnlockServer(identifier);
        }

        private void Logout( string token )
        {
            var authenticationClient = new AuthenticationServiceProvider.Client( _server.Channel, new DiscoveryExecutionManager() );
            authenticationClient.Logout( token );
        }
    }
}

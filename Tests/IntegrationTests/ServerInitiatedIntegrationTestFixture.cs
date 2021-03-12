using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tecan.Sila2.Authentication.AuthenticationService;
using Tecan.Sila2.ConnectionConfiguration.ConnectionConfigurationService;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.ServerPooling;
using Tecan.Sila2.Tests;

namespace Tecan.Sila2.IntegrationTests
{
    // Integration tests do not work on CI server, unfortunately
    [TestFixture( Category = "IntegrationTests" )]
    public class ServerInitiatedIntegrationTestFixture : IntegrationTestFixtureBase
    {
        private PoolServer _poolServer;
        private ServerProvider _serverProvider;

        protected override ServerData FindServer(Guid serverUuid)
        {
            var logging = new TestLoggingFactory();
            _serverProvider = new ServerProvider( logging , new FileCertificateProvider() );
            _poolServer = new PoolServer( logging, _serverProvider );
            var tcs = new TaskCompletionSource<ServerData>();
            _poolServer.ServerConnected += ( o, e ) => tcs.SetResult( e.Server );
            _serverProvider.Configure("127.0.0.1", 50053);
            _serverProvider.Start();

            var testServer = base.FindServer(serverUuid);

            var authenticationClient = new AuthenticationServiceClient( testServer.Channel, new DiscoveryExecutionManager() );
            var token = authenticationClient.Login( "admin", "admin", testServer.Config.Uuid.ToString(), new List<string>() );
            var client = new ConnectionConfigurationServiceClient( testServer.Channel, new TestAuthLockingExecutionManager(null, token.AccessToken, testServer.Channel) );

            client.EnableServerInitiatedConnectionMode();
            client.ConnectSiLAClient( "TestClient", "127.0.0.1", 50053, false );

            tcs.Task.Wait( 1000 );
            return tcs.Task.Result;
        }

        public override void StopTestServer()
        {
            base.StopTestServer();
            _serverProvider?.ShutdownAsync().Wait();
        }
    }
}

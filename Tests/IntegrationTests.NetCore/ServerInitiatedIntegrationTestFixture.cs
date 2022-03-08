using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Tecan.Sila2.Authentication.AuthenticationService;
using Tecan.Sila2.Client;
using Tecan.Sila2.ConnectionConfiguration.ConnectionConfigurationService;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Security;
using Tecan.Sila2.Server;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.IntegrationTests
{
    // Integration tests do not work on CI server, unfortunately
    [TestFixture( Category = "IntegrationTests" )]
    public class ServerInitiatedIntegrationTestFixture : ClientInitiatedIntegrationTestFixture
    {
        private PoolServer _poolServer;
        private IHost _host;

        protected override ServerData FindServer( Guid serverUuid )
        {
            var configurationBuilder = new ServiceConfigurationBuilder( new NoCertificateProvider() );
            _poolServer = new PoolServer( configurationBuilder );
            configurationBuilder.Configure( "127.0.0.1", 50053 );
            _host = Host.CreateDefaultBuilder()
                    .ConfigureServices( ( _, services ) =>
                    {
                        services.AddSingleton<IServerPool>( _poolServer );
                        services.AddSingleton<IServiceConfigurationBuilder<IServerPool>>( configurationBuilder );
                        services.AddSila2ServerPool();
                    } )
                    .ConfigureWebHostDefaults( webHostBuilder =>
                    {
                        webHostBuilder.ConfigureKestrel( options =>
                        {
                            configurationBuilder.ConfigureKestrel( options );
                        } );
                        webHostBuilder.Configure( appBuilder =>
                        {
                            appBuilder.UseRouting();
                            appBuilder.UseEndpoints( endpoints => endpoints.MapSila2ServerPool() );
                        } );
                    } )
                    .Build();
            _host.Start();

            var tcs = new TaskCompletionSource<ServerData>();
            _poolServer.ServerConnected += ( o, e ) =>
            {
                tcs.SetResult( e.Server );
            };

            var testServer = base.FindServer( serverUuid );

            var authenticationClient = new AuthenticationServiceClient( testServer.Channel, new DiscoveryExecutionManager() );
            var token = authenticationClient.Login( "admin", "admin", testServer.Config.Uuid.ToString(), new List<string>() );
            var client = new ConnectionConfigurationServiceClient( testServer.Channel, new TestAuthLockingExecutionManager( null, token.AccessToken, testServer.Channel ) );

            Console.WriteLine( "Requesting server to connect to pool." );
            client.EnableServerInitiatedConnectionMode();
            client.ConnectSiLAClient( "TestClient", "http://127.0.0.1", 50053, false );

            tcs.Task.Wait( 10000 );
            Console.WriteLine( "Waiting complete." );
            Assert.That( tcs.Task.IsCompleted, "Server connected to pool." );
            return tcs.Task.Result;
        }

        public override void StopTestServer()
        {
            base.StopTestServer();
            if(_host != null)
            {
                try
                {
                    _host.StopAsync().Wait();
                }
                finally
                {
                    _host.Dispose();
                }
            }
        }

        private class NoCertificateProvider : IServerCertificateProvider
        {
            public CertificateContext CreateContext( Guid serverGuid )
            {
                return null;
            }

            public CertificateContext CreateContext()
            {
                return null;
            }
        }
    }
}

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

        protected override ServerData FindServer(Guid serverUuid)
        {
            var configurationBuilder = new ServiceConfigurationBuilder( new FileCertificateProvider() );
            _poolServer = new PoolServer( configurationBuilder );
            configurationBuilder.Configure( "127.0.0.1", 50053 );
            _host = Host.CreateDefaultBuilder()
                    .ConfigureServices( ( _, services ) =>
                    {
                        services.AddSingleton( _poolServer );
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

            client.EnableServerInitiatedConnectionMode();
            client.ConnectSiLAClient( "TestClient", "127.0.0.1", 50053, false );

            tcs.Task.Wait( 1000 );
            return tcs.Task.Result;
        }
        //protected override ProcessStartInfo GetTestServerStartInfo( string payloadArguments )
        //{
        //    AppContext.SetSwitch( "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true );
        //    return null;
        //}

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
    }
}

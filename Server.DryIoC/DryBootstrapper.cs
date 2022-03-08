using CommandLine;
using Common.Logging;
using DryIoc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tecan.Sila2.Logging;
using Tecan.Sila2.Security;
using Tecan.Sila2.Server.Binary;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes a bootstrapper that starts a SiLA2 server with DryIoC, reading components using MEF annotations
    /// </summary>
    public class DryBootstrapper
    {
        private ISiLAServer _server;
        private ILog _channel;
        private IHost _host;

        /// <summary>
        /// Starts the server with the given command line parameters
        /// </summary>
        /// <param name="args">The command line parameters</param>
        public static DryBootstrapper Start( string[] args )
        {
            var bootstrapper = new DryBootstrapper();
            if(!Start( args, bootstrapper ))
            {
                throw new InvalidOperationException( "Server could not be started." );
            }
            return bootstrapper;
        }

        /// <summary>
        /// Runs the server
        /// </summary>
        public void RunUntilReadline()
        {
            Console.ReadLine();
        }

        /// <summary>
        /// The protocols the server should accept
        /// </summary>
        protected virtual HttpProtocols Protocols => HttpProtocols.Http2;

        /// <summary>
        /// Starts the server with the given command line parameters
        /// </summary>
        /// <param name="args">The command line parameters</param>
        /// <param name="bootstrapper">The bootstrapper instance that should be used</param>
        public static bool Start( string[] args, DryBootstrapper bootstrapper )
        {
            if(bootstrapper == null) throw new ArgumentNullException( nameof( bootstrapper ) );

            return Parser.Default.ParseArguments<ServerCommandLineArguments>( args )
                .MapResult( bootstrapper.StartServer, bootstrapper.HandleError );
        }

        /// <summary>
        /// Starts the server with the given parsed command line parameters
        /// </summary>
        /// <param name="args">The parsed command line parameters</param>
        /// <returns>True, if the server was started successfully, otherwise False</returns>
        public virtual bool StartServer( ServerCommandLineArguments args )
        {
            AppDomain.CurrentDomain.ProcessExit += TryShutdownServer;
            var selfInfo = GetServerInformation();
            try
            {
                _channel = LogManager.GetLogger<Bootstrapper>();

                ServiceConfigurationBuilder configurationBuilder = null;
                var startup = new ServerStartInformation( selfInfo, args.Port, args.InterfaceFilter, args.ServerUuid, args.Name );

                _host = Host.CreateDefaultBuilder()
                    .UseDryIoc( container =>
                     {
                         container.RegisterInstance( startup );
                         OnLoadComponents( container );
                         ApplyDefaultsIfMissing( container );
                     } )
                    .ConfigureServices( services =>
                    {
                        services.AddSila2();
                        OnConfigureServices( services );
                    } )
                    .ConfigureWebHostDefaults( webHostBuilder =>
                    {
                        webHostBuilder.ConfigureKestrel( options =>
                        {
                            configurationBuilder?.ConfigureKestrel( options, Protocols );
                            OnConfigureKestrel( options );
                        } );
                        webHostBuilder.Configure( appBuilder =>
                        {
                            appBuilder.UseRouting();
                            OnConfigureApplication( appBuilder );
                            appBuilder.UseEndpoints( endpoints =>
                            {
                                endpoints.MapSila2();
                                OnConfigureEndpoints( endpoints );
                            } );
                        } );
                    } )
                    .Build();

                if(LogManager.Adapter is Common.Logging.Simple.NoOpLoggerFactoryAdapter || LogManager.Adapter == null)
                {
                    _host.Services.InitializeLogging();
                    _channel = LogManager.GetLogger<Bootstrapper>();
                }

                configurationBuilder = _host.Services.GetRequiredService<IServiceConfigurationBuilder<ISiLAServer>>() as ServiceConfigurationBuilder;
                _server = _host.Services.GetRequiredService<ISiLAServer>();

                _host.Start();
                return true;
            }
            catch(ContainerException e)
            {
                LogError( _channel, e );
                if(e.CollectedExceptions != null)
                {
                    foreach(var compositionError in e.CollectedExceptions)
                    {
                        LogError( _channel, compositionError );
                    }
                }
            }
            catch(Exception e)
            {
                LogError( _channel, e );
            }
            return false;
        }

        /// <summary>
        /// Gets the service provider used by the ASP.NET Core server
        /// </summary>
        public IServiceProvider ServiceProvider => _host?.Services;

        /// <summary>
        /// Gets called when the server configures services
        /// </summary>
        /// <param name="serviceCollection">The service collection to configure</param>
        protected virtual void OnConfigureServices( IServiceCollection serviceCollection )
        {
        }

        /// <summary>
        /// Gets called when the server is configured
        /// </summary>
        /// <param name="applicationBuilder">The application builder</param>
        protected virtual void OnConfigureApplication( IApplicationBuilder applicationBuilder )
        {
        }

        /// <summary>
        /// Gets called when the Kestrel server is configured
        /// </summary>
        /// <param name="kestrelOptions">A Kestrel configuration object</param>
        protected virtual void OnConfigureKestrel( KestrelServerOptions kestrelOptions )
        {
        }

        /// <summary>
        /// Gets called when the endpoints are configured
        /// </summary>
        protected virtual void OnConfigureEndpoints( IEndpointRouteBuilder endpoints )
        {
        }

        private static void LogError( ILog channel, Exception e )
        {
            if(channel != null)
            {
                channel.Error( "Server creation/initialization exception: ", e );
            }
            else
            {
                Console.Error.WriteLine( $"Error starting server: {e.Message}" );
            }
        }



        private void TryShutdownServer( object sender, EventArgs e )
        {
            try
            {
                ShutdownServer();
            }
            catch(Exception exception)
            {
                if(_channel != null)
                {
                    _channel.Error( "Error shutting down server", exception );
                }
                else
                {
                    Console.Error.WriteLine( $"Error shutting down server: {exception.Message}." );
                }
            }
        }

        /// <summary>
        /// Shuts down the SiLA2 Server
        /// </summary>
        public virtual void ShutdownServer()
        {
            try
            {
                _server?.ShutdownServer();
                _host?.StopAsync().Wait();
            }
            catch(Exception exception)
            {
                _channel?.Error( "Error shutting down server", exception );
            }
            finally
            {
                _host?.Dispose();
            }
        }

        /// <summary>
        /// Instructs to load the default contents of the container
        /// </summary>
        /// <param name="container">The container that needs to be filled</param>
        protected virtual void OnLoadComponents( IContainer container )
        {
            container.LoadComponentsFromApplicationDirectory();
        }

        /// <summary>
        /// Registers default implementations for commonly required interfaces, if the composition container does not already contain implementations
        /// </summary>
        /// <param name="container">The composition container</param>
        protected void ApplyDefaultsIfMissing( IContainer container )
        {
            container.AddSila2Defaults();
        }

        /// <summary>
        /// Gets the server information for the current server
        /// </summary>
        /// <returns>An object that contains the server information for this server</returns>
        protected virtual ServerInformation GetServerInformation()
        {
            return ServerInformationFactory.GetServerInformation();
        }

        /// <summary>
        /// Handles errors at server startup
        /// </summary>
        /// <param name="obj"></param>
        public virtual bool HandleError( IEnumerable<CommandLine.Error> obj )
        {
            Environment.ExitCode = 1;
            return false;
        }
    }
}

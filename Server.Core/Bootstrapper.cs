using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Reflection;
using System.Text;
using CommandLine;
using Common.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tecan.Sila2.Logging;
using Tecan.Sila2.Security;
using Tecan.Sila2.Server.Binary;
using Tecan.Sila2.Server.ServerPooling;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// A helper class to bootstrap a complete server
    /// </summary>
    public class Bootstrapper
    {
        private CompositionContainer _container;
        private ISiLAServer _server;
        private ILog _channel;
        private IHost _host;


        /// <summary>
        /// Starts the server with the given command line parameters
        /// </summary>
        /// <param name="args">The command line parameters</param>
        public static Bootstrapper Start( string[] args )
        {
            var bootstrapper = new Bootstrapper();
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
        /// Starts the server with the given command line parameters
        /// </summary>
        /// <param name="args">The command line parameters</param>
        /// <param name="bootstrapper">The bootstrapper instance that should be used</param>
        public static bool Start( string[] args, Bootstrapper bootstrapper )
        {
            if(bootstrapper == null) throw new ArgumentNullException( nameof( bootstrapper ) );

            return Parser.Default.ParseArguments<ServerCommandLineArguments>( args )
                .MapResult( bootstrapper.StartServer, bootstrapper.HandleError );
        }

        /// <summary>
        /// The protocols the server should accept
        /// </summary>
        protected virtual HttpProtocols Protocols => HttpProtocols.Http2;

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
                var replaceLogging = false;
                replaceLogging = InitializeLogging();

                _channel = LogManager.GetLogger<Bootstrapper>();

                ServiceConfigurationBuilder configurationBuilder = null;

                _host = Host.CreateDefaultBuilder()
                    .ConfigureServices( services =>
                    {

                        _container = CreateContainer();
                        var interfaceFilter = args.InterfaceFilter;
                        if(interfaceFilter == "None")
                        {
                            interfaceFilter = null;
                        }
                        var startup = new ServerStartInformation( selfInfo, args.Port, args.InterfaceFilter, args.ServerUuid, args.Name );
                        _container.ComposeExportedValue( startup );
                        var certificateProvider = _container.GetExportedValue<IServerCertificateProvider>();
                        configurationBuilder = new ServiceConfigurationBuilder( certificateProvider );
                        _container.ComposeExportedValue<IServiceConfigurationBuilder<ISiLAServer>>( configurationBuilder );
                        _server = _container.GetExportedValue<ISiLAServer>();

                        _server.StartServer();

                        services.AddSingleton( certificateProvider );
                        services.AddSingleton( _server );
                        services.AddSingleton<IServiceConfigurationBuilder<ISiLAServer>>( configurationBuilder );
                        var features = _container.GetExportedValues<IFeatureProvider>();
                        foreach(var featureProvider in features)
                        {
                            services.AddSingleton( featureProvider );
                        }
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
                            appBuilder.UseEndpoints( endpoints => endpoints.MapSila2() );
                        } );
                    } )
                    .Build();

                if(LogManager.Adapter is Common.Logging.Simple.NoOpLoggerFactoryAdapter || LogManager.Adapter == null || replaceLogging)
                {
                    _host.Services.InitializeLogging();
                    _channel = LogManager.GetLogger<Bootstrapper>();
                }

                _host.Start();
                return true;
            }
            catch(CompositionException e)
            {
                LogError( _channel, e );
                foreach(var compositionError in e.Errors)
                {
                    LogError( _channel, compositionError.Exception );
                }

                foreach(var rootCause in e.RootCauses)
                {
                    LogError( _channel, rootCause );
                }
            }
            catch(Exception e)
            {
                LogError( _channel, e );
            }
            return false;
        }
        private static bool InitializeLogging()
        {
            if(LogManager.Adapter is Common.Logging.Simple.NoOpLoggerFactoryAdapter || LogManager.Adapter == null)
            {
                LogManager.Adapter = new ConsoleLogging( Common.Logging.LogLevel.Debug );
                return true;
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
        /// The composition container used by this bootstrapper
        /// </summary>
        public CompositionContainer Container => _container;

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

            _container?.Dispose();
        }

        /// <summary>
        /// Creates the container for the created server
        /// </summary>
        /// <returns>A MEF composition container</returns>
        protected virtual CompositionContainer CreateContainer()
        {
            var assemblyLocation = Assembly.GetEntryAssembly().Location;
            ComposablePartCatalog catalog = new DirectoryCatalog( Path.GetDirectoryName( assemblyLocation ) );
            if(assemblyLocation.EndsWith( ".exe" ))
            {
                catalog = new AggregateCatalog( catalog, new AssemblyCatalog( Assembly.GetEntryAssembly() ) );
            }

            var container = new CompositionContainer( catalog );

            ApplyDefaultsIfMissing( container );

            return container;
        }

        /// <summary>
        /// Registers default implementations for commonly required interfaces, if the composition container does not already contain implementations
        /// </summary>
        /// <param name="container">The composition container</param>
        protected void ApplyDefaultsIfMissing( CompositionContainer container )
        {
            if(container.GetExportedValueOrDefault<IFileManagement>() == null)
            {
                container.ComposeExportedValue<IFileManagement>( new FileManagement() );
            }

            if(container.GetExportedValueOrDefault<IConfigurationStore>() == null)
            {
                container.ComposeExportedValue<IConfigurationStore>( new FileConfigurationStore() );
            }

            if(container.GetExportedValueOrDefault<IServerCertificateProvider>() == null)
            {
                container.ComposeExportedValue<IServerCertificateProvider>( new FileCertificateProvider() );
            }

            if(container.GetExportedValueOrDefault<IServerPoolConnectionFactory>() == null)
            {
                container.ComposeExportedValue<IServerPoolConnectionFactory>( new DefaultServerPoolConnectionFactory() );
            }
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
        public virtual bool HandleError( IEnumerable<Error> obj )
        {
            Environment.ExitCode = 1;
            return false;
        }
    }
}

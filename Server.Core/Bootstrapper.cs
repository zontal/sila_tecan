using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Reflection;
using System.Text;
using CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Security;
using Tecan.Sila2.Server.Binary;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// A helper class to bootstrap a complete server
    /// </summary>
    public class Bootstrapper
    {
        private CompositionContainer _container;
        private ISiLAServer _server;
        private ILoggingChannel _channel;
        private IHost _host;

        /// <summary>
        /// Starts the server with the given command line parameters
        /// </summary>
        /// <param name="args">The command line parameters</param>
        public static bool Start( string[] args )
        {
            var bootstrapper = new Bootstrapper();

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
                _container = CreateContainer();
                var logging = _container.GetExportedValue<ILoggingChannelFactory>();
                if(Common.Logging.LogManager.Adapter is Common.Logging.Simple.NoOpLoggerFactoryAdapter || Common.Logging.LogManager.Adapter == null)
                {
                    Common.Logging.LogManager.Adapter = new LoggerFactoryAdapter( logging );
                }

                _channel = logging.CreateChannel( typeof( Bootstrapper ) );
                var interfaceFilter = args.InterfaceFilter;
                if(interfaceFilter == "None")
                {
                    interfaceFilter = null;
                }
                var startup = new ServerStartInformation( selfInfo, args.Port, args.InterfaceFilter, args.ServerUuid, args.Name );
                _container.ComposeExportedValue( startup );
                var serviceHandlerRepository = new ServiceHandlerRepository();
                _container.ComposeExportedValue<IServiceHandlerRepository<ISiLAServer>>( serviceHandlerRepository );
                _server = _container.GetExportedValue<ISiLAServer>();

                _server.StartServer();

                _host = Host.CreateDefaultBuilder()
                    .ConfigureServices((_,services) =>
                    {
                        services.AddSingleton( _server );
                        services.AddSingleton( serviceHandlerRepository );
                        var features = _container.GetExportedValues<IFeatureProvider>();
                        foreach(var featureProvider in features)
                        {
                            services.AddSingleton( featureProvider );
                        }
                    })
                    .ConfigureWebHostDefaults( webHostBuilder => webHostBuilder.UseStartup<Startup>() )
                    .Build();
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

        /// <summary>
        /// The composition container used by this bootstrapper
        /// </summary>
        public CompositionContainer Container => _container;

        private static void LogError( ILoggingChannel channel, Exception e )
        {
            if(channel != null)
            {
                channel.LogException( "Server creation/initialization exception: ".ToString, e );
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
                    _channel.LogException( "Error shutting down server".ToString, exception );
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
                _host.StopAsync().Wait();
            }
            catch(Exception exception)
            {
                _channel?.LogException( "Error shutting down server".ToString, exception );
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
            if(container.GetExportedValueOrDefault<ILoggingChannelFactory>() == null)
            {
                container.ComposeExportedValue<ILoggingChannelFactory>( new ConsoleLogging() );
            }

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
        }

        /// <summary>
        /// Gets the server information for the current server
        /// </summary>
        /// <returns>An object that contains the server information for this server</returns>
        protected virtual ServerInformation GetServerInformation()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var entryAssemblyName = entryAssembly.GetName();
            var serverType = entryAssembly.GetCustomAttribute<ServerTypeAttribute>();
            var product = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>();
            return new ServerInformation(
                CreateServerTypeString( serverType?.ServerType ?? product?.Product ?? entryAssemblyName.Name ),
                entryAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description,
                entryAssembly.GetCustomAttribute<VendorUriAttribute>()?.VendorUri,
                entryAssemblyName.Version.ToString( 2 ) );
        }

        private string CreateServerTypeString( string input )
        {
            var sb = new StringBuilder();
            foreach(var character in input)
            {
                if((character >= 'a' && character <= 'z') ||
                   (character >= 'A' && character <= 'Z') ||
                   char.IsDigit( character ) || character == '.')
                {
                    sb.Append( character );
                }
                else if(character == 'ä')
                {
                    sb.Append( "ae" );
                }
                else if(character == 'ö')
                {
                    sb.Append( "oe" );
                }
                else if(character == 'ü')
                {
                    sb.Append( "ue" );
                }
            }

            return sb.ToString();
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
        private class Startup
        {
            // This method gets called by the runtime. Use this method to add services to the container.
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
            public void ConfigureServices( IServiceCollection services )
            {
                services.AddSila2();
            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure( IApplicationBuilder app, IWebHostEnvironment env )
            {
                if(env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseRouting();

                app.UseEndpoints( endpoints =>
                {
                    endpoints.MapGet( "/", async context =>
                    {
                        await context.Response.WriteAsync( "This is a SiLA2 server built using the Tecan SiLA2 SDK." );
                    } );
                    endpoints.MapSila2();
                } );
            }
        }
    }
}

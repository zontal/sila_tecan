using CommandLine;
using Common.Logging;
using DryIoc;
using DryIoc.MefAttributedModel;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
        private IContainer _container;
        private ISiLAServer _server;
        private ILog _channel;
        private IHost _host;

        /// <summary>
        /// Starts the server with the given command line parameters
        /// </summary>
        /// <param name="args">The command line parameters</param>
        public static bool Start( string[] args )
        {
            var bootstrapper = new DryBootstrapper();

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

                _container = CreateContainer();

                _container.Register<IServiceConfigurationBuilder<ISiLAServer>, ServiceConfigurationBuilder>();
                _container.RegisterInstance( startup );

                _host = Host.CreateDefaultBuilder()
                    .UseServiceProviderFactory(new DryIocServiceProviderFactory(_container))
                    .ConfigureServices( services =>
                    {
                        services.AddSila2();
                    } )
                    .ConfigureWebHostDefaults( webHostBuilder =>
                    {
                        webHostBuilder.ConfigureKestrel( options =>
                        {
                            configurationBuilder?.ConfigureKestrel( options );
                        } );
                        webHostBuilder.Configure( appBuilder =>
                        {
                            appBuilder.UseRouting();
                            appBuilder.UseEndpoints( endpoints => endpoints.MapSila2() );
                        } );
                    } )
                    .Build();

                if(LogManager.Adapter is Common.Logging.Simple.NoOpLoggerFactoryAdapter || LogManager.Adapter == null )
                {
                    var loggerFactory = _host.Services.GetRequiredService<ILoggerFactory>();
                    LogManager.Adapter = new AspNetAdapter( loggerFactory );
                    _channel = LogManager.GetLogger<Bootstrapper>();
                }

                configurationBuilder = _container.Resolve<IServiceConfigurationBuilder<ISiLAServer>>() as ServiceConfigurationBuilder;
                _server = _container.Resolve<ISiLAServer>();

                _server.StartServer();

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

        /// <summary>
        /// The composition container used by this bootstrapper
        /// </summary>
        public IContainer Container => _container;



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
        protected virtual IContainer CreateContainer()
        {
            var assemblyLocation = Assembly.GetEntryAssembly().Location;

            var container = new Container().WithMef();

            OnLoadComponents( container, Path.GetDirectoryName( assemblyLocation ) );

            ApplyDefaultsIfMissing( container );

            return container;
        }

        /// <summary>
        /// Instructs to load the default contents of the container
        /// </summary>
        /// <param name="container">The container that needs to be filled</param>
        /// <param name="path">The default path where to look for components</param>
        protected virtual void OnLoadComponents( IContainer container, string path)
        {
            LoadComponents( container, path, null );
        }

        /// <summary>
        /// Loads the exports contained in assemblies in the given folder into the provided container
        /// </summary>
        /// <param name="container">The DryIoc container</param>
        /// <param name="path">The folder path</param>
        /// <param name="filter">A filter or null to use the default filter *.dll</param>
        protected void LoadComponents( IContainer container, string path, string filter)
        {
            foreach(var assemblyFile in Directory.EnumerateFiles( path, filter ?? "*.dll" ))
            {
                try
                {
                    LoadComponentsFromAssembly( container, Assembly.LoadFrom( Path.Combine( path, assemblyFile ) ) );
                }
                catch(BadImageFormatException)
                {
                    // assembly is not a .NET DLL and therefore safe to ignore
                }
            }
        }

        /// <summary>
        /// Loads the exports from the given assembly into the provided container
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="assembly">The assembly that contains the components</param>
        protected void LoadComponentsFromAssembly( IContainer container, Assembly assembly )
        {
            container.RegisterExports( assembly );
        }

        /// <summary>
        /// Registers default implementations for commonly required interfaces, if the composition container does not already contain implementations
        /// </summary>
        /// <param name="container">The composition container</param>
        protected void ApplyDefaultsIfMissing( IContainer container )
        {
            if(!container.IsRegistered<IFileManagement>())
            {
                container.RegisterInstance<IFileManagement>( new FileManagement() );
            }

            if(!container.IsRegistered<IConfigurationStore>())
            {
                container.RegisterInstance<IConfigurationStore>( new FileConfigurationStore() );
            }

            if(!container.IsRegistered<IServerCertificateProvider>())
            {
                container.RegisterInstance<IServerCertificateProvider>( new FileCertificateProvider() );
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
        public virtual bool HandleError( IEnumerable<CommandLine.Error> obj )
        {
            Environment.ExitCode = 1;
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using CommandLine;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Discovery;
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

        /// <summary>
        /// Starts the server with the given command line parameters
        /// </summary>
        /// <param name="args">The command line parameters</param>
        public static void Start(string[] args )
        {
            var bootstrapper = new Bootstrapper();

            Parser.Default.ParseArguments<ServerCommandLineArguments>(args)
                .WithNotParsed(bootstrapper.HandleError)
                .WithParsed(parsedArgs =>
                {
                    bootstrapper.StartServer(parsedArgs);
                });
        }

        /// <summary>
        /// Starts the server with the given parsed command line parameters
        /// </summary>
        /// <param name="args">The parsed command line parameters</param>
        public void StartServer(ServerCommandLineArguments args)
        {
            AppDomain.CurrentDomain.ProcessExit += TryShutdownServer;
            var selfInfo = GetServerInformation();
            try
            {
                _container = CreateContainer();
                var logging = _container.GetExportedValue<ILoggingChannelFactory>();
                if( Common.Logging.LogManager.Adapter is Common.Logging.Simple.NoOpLoggerFactoryAdapter || Common.Logging.LogManager.Adapter == null )
                {
                    Common.Logging.LogManager.Adapter = new LoggerFactoryAdapter( logging );
                }

                _channel = logging.CreateChannel( typeof(Bootstrapper) );
                var networkInterface = FindNetworkInterface( args.InterfaceNameOrCIDR, logging );
                var startup = new ServerStartInformation( selfInfo, args.Port, networkInterface, args.ServerUuid );
                _container.ComposeExportedValue( startup );
                _server = _container.GetExportedValue<ISiLAServer>();

                var features = _container.GetExportedValues<IFeatureProvider>();
                foreach( var feature in features )
                {
                    _server.AddFeature( feature );
                }

                _server.StartServer();
                _channel.Log( () => "Server listening on port " + args.Port, Severity.Info );
            }
            catch( CompositionException e )
            {
                LogError(_channel, e);
                foreach( var compositionError in e.Errors )
                {
                    LogError(_channel, compositionError.Exception );
                }

                foreach( var rootCause in e.RootCauses )
                {
                    LogError(_channel, rootCause );
                }
            }
            catch( Exception e )
            {
                LogError(_channel, e);
            }
        }

        private static void LogError(ILoggingChannel channel, Exception e)
        {
            if (channel != null)
            {
                channel.LogException("Server creation/initialization exception: ".ToString, e);
            }
            else
            {
                Console.Error.WriteLine($"Error starting server: {e.Message}");
            }
        }

        private void TryShutdownServer(object sender, EventArgs e)
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
                    Console.Error.WriteLine($"Error shutting down server: {exception.Message}.");
                }
            }
        }

        /// <summary>
        /// Shuts down the SiLA2 Server
        /// </summary>
        public virtual void ShutdownServer()
        {
            _server.ShutdownServer();
            _container.Dispose();
        }

        private static NetworkInterface FindNetworkInterface(string interfaceNameOrCidr, ILoggingChannelFactory logging)
        {
            NetworkInterface networkInterface;
            if( interfaceNameOrCidr != null )
            {
                try
                {
                    var networking = new Networking( logging );
                    networkInterface = networking.GetNetworkInterface( interfaceNameOrCidr );
                }
                catch( ArgumentNullException )
                {
                    networkInterface = null;
                }
            }
            else
            {
                networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault( ni => ni.SupportsMulticast && ni.OperationalStatus == OperationalStatus.Up );
            }

            return networkInterface;
        }

        /// <summary>
        /// Creates the container for the created server
        /// </summary>
        /// <returns>A MEF composition container</returns>
        protected virtual CompositionContainer CreateContainer()
        {
            var assemblyLocation = Assembly.GetEntryAssembly().Location;
            ComposablePartCatalog catalog = new DirectoryCatalog(Path.GetDirectoryName(assemblyLocation));
            if (assemblyLocation.EndsWith(".exe"))
            {
                catalog = new AggregateCatalog(catalog, new AssemblyCatalog(Assembly.GetEntryAssembly()));
            }

            var container = new CompositionContainer(catalog);

            if (container.GetExportedValueOrDefault<ILoggingChannelFactory>() == null)
            {
                container.ComposeExportedValue<ILoggingChannelFactory>( new ConsoleLogging() );
            }

            if( container.GetExportedValueOrDefault<IServerConnector>() == null )
            {
                container.ComposeExportedValue<IServerConnector>(new ServerConnector(new DiscoveryExecutionManager(), container.GetExportedValue<ILoggingChannelFactory>()));
            }

            if (container.GetExportedValueOrDefault<IFileManagement>() == null)
            {
                container.ComposeExportedValue<IFileManagement>(new FileManagement());
            }

            if( container.GetExportedValueOrDefault<IConfigurationStore>() == null )
            {
                container.ComposeExportedValue<IConfigurationStore>(new FileConfigurationStore());
            }

            return container;
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
        protected virtual void HandleError(IEnumerable<Error> obj)
        {
            Environment.Exit(1);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using CommandLine;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.MAP.Contracts.Services.Logging.Configuration;
using Tecan.Sila2.Server.Binary;

namespace Tecan.Sila2.Server
{
    public class Bootstrapper
    {
        private CompositionContainer _container;
        private ISiLAServer _server;

        public static void Start(string[] args)
        {
            var bootstrapper = new Bootstrapper();

            Parser.Default.ParseArguments<ServerCommandLineArguments>(args)
                .WithNotParsed(bootstrapper.HandleError)
                .WithParsed(parsedArgs =>
                {
                    bootstrapper.StartServer(parsedArgs);
                });
        }

        public void StartServer(ServerCommandLineArguments args)
        {
            AppDomain.CurrentDomain.ProcessExit += (o,e) => ShutdownServer();
            var selfInfo = GetServerInformation();
            ILoggingChannel channel = null;
            try
            {
                _container = CreateContainer();
                var logging = _container.GetExportedValue<ILoggingChannelFactory>();
                channel = logging.CreateChannel( typeof(Bootstrapper) );
                var networkInterface = FindNetworkInterface( args.InterfaceNameOrCIDR, logging );
                var startup = new ServerStartupInfo( selfInfo, args.Port, networkInterface, args.ServerUuid );
                _container.ComposeExportedValue<IServerStartInformation>( startup );
                _server = _container.GetExportedValue<ISiLAServer>();

                var features = _container.GetExportedValues<IFeatureProvider>();
                foreach( var feature in features )
                {
                    _server.AddFeature( feature );
                }

                _server.StartServer();
                channel.Log( () => "Server listening on port " + args.Port, Severity.Info );
            }
            catch( Exception e )
            {
                if( channel != null )
                {
                    channel.LogException( "Server creation/initialization exception: ".ToString, e );
                }
                else
                {
                    Console.Error.WriteLine($"Error starting server: {e.Message}");
                }
            }
        }

        public void ShutdownServer()
        {
            _server.ShutdownServer();
            _container.Dispose();
        }

        private static NetworkInterface FindNetworkInterface(string interfaceNameOrCIDR, ILoggingChannelFactory logging)
        {
            NetworkInterface networkInterface;
            if( interfaceNameOrCIDR != null )
            {
                try
                {
                    var networking = new Networking( logging );
                    networkInterface = networking.GetNetworkInterface( interfaceNameOrCIDR );
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

        protected virtual CompositionContainer CreateContainer()
        {
            var assemblyLocation = Assembly.GetEntryAssembly().Location;
            ComposablePartCatalog catalog = new DirectoryCatalog(Path.GetDirectoryName(assemblyLocation));
            if (assemblyLocation.EndsWith(".exe"))
            {
                catalog = new AggregateCatalog(catalog, new AssemblyCatalog(Assembly.GetEntryAssembly()));
            }

            var container = new CompositionContainer(catalog);

            if (container.GetExportedValueOrDefault<IFileManagement>() == null)
            {
                container.ComposeExportedValue<IFileManagement>(new FileManagement());
            }

            if (container.GetExportedValueOrDefault<ILoggingChannelFactory>() == null)
            {
                container.ComposeExportedValue<ILoggingChannelFactory>( new ConsoleLogging() );
            }

            return container;
        }

        protected virtual ServerInformation GetServerInformation()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var entryAssemblyName = entryAssembly.GetName();
            var product = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>();
            return new ServerInformation(
                product?.Product ?? entryAssemblyName.Name, 
                entryAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description,
                entryAssembly.GetCustomAttribute<VendorUriAttribute>()?.VendorUri,
                entryAssemblyName.Version.ToString());
        }

        protected virtual void HandleError(IEnumerable<Error> obj)
        {
            Environment.Exit(1);
        }
    }
}

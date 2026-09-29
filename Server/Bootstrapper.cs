using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Common.Logging;
using Common.Logging.Simple;
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
        private IServerProvider _grpcServer;
        private ILog _channel;


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
        /// Runs the server until the process is terminated through SIGTERM
        /// </summary>
        public void RunUntilSigInt()
        {
            var tcs = new TaskCompletionSource<bool>();
            var sigintReceived = false;
            Console.CancelKeyPress += (_, ea) =>
            {
                ea.Cancel = true;
                tcs.SetResult(true);
                sigintReceived = true;
            };
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                if (!sigintReceived)
                {
                    tcs.SetResult(false);
                }
            };
            tcs.Task.Wait();
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
                if(LogManager.Adapter is NoOpLoggerFactoryAdapter || LogManager.Adapter == null)
                {
                    LogManager.Adapter = new ConsoleLogging( LogLevel.Debug );
                }

                _channel = LogManager.GetLogger<Bootstrapper>();
                var interfaceFilter = args.InterfaceFilter;
                if(string.Equals(interfaceFilter, "none", StringComparison.OrdinalIgnoreCase))
                {
                    interfaceFilter = null;
                }
                var certificateProvider = _container.GetExportedValue<IServerCertificateProvider>();
                certificateProvider.Initialize(args.CertificatePath);
                var startup = new ServerStartInformation( selfInfo, args.Port, args.InterfaceFilter, !args.NoDiscovery, args.ServerUuid, args.Name, args.ConfigName );
                _container.ComposeExportedValue( startup );
                _server = _container.GetExportedValue<ISiLAServer>();
                _grpcServer = _container.GetExportedValue<IServerProvider>();

                var features = _container.GetExportedValues<IFeatureProvider>();
                foreach(var feature in features)
                {
                    _server.AddFeature( feature );
                }

                _grpcServer.Start();
                _server.StartServer();
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
                _grpcServer.ShutdownAsync().Wait();
            }
            catch(Exception exception)
            {
                _channel?.Error( "Error shutting down server", exception );
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
                container.ComposeExportedValue<IServerPoolConnectionFactory>( new PlaintextPoolConnectionFactory() );
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

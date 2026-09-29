using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Server.Binary;
using Tecan.Sila2.Server.ConnectionConfiguration;
using Tecan.Sila2.Server.ServerPooling;
using Tecan.Sila2.Server.ServiceDefinition;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server
{
    public partial class SiLAServer
    {
        private readonly IServerProvider _serverProvider;

        /// <summary>
        /// Creates a new instance of the SiLA2Server class, adds the SiLAService Feature implementation, creates a gRPC Server and registers the server for Server discovery.
        /// </summary>
        /// <param name="startupInfo">The information the server requires to start up</param>
        /// <param name="configurationStore">An object to read and write server configuration</param>
        /// <param name="interceptors">Interceptors to intercept any commands issued to the server</param>
        /// <param name="fileManagement">The file management used by this server</param>
        /// <param name="serverProvider">The actual gRPC server the SiLA2 server should be running on</param>
        /// <param name="serverPoolConnectionFactory">A component that creates a callinvoker for a request to connect to a pool server</param>
        [ImportingConstructor]
        public SiLAServer( ServerStartInformation startupInfo, IConfigurationStore configurationStore, [ImportMany] IEnumerable<IRequestInterceptor> interceptors, IFileManagement fileManagement, IServerProvider serverProvider, IServerPoolConnectionFactory serverPoolConnectionFactory )
        {
            _configurationStore = configurationStore;
            _upload = new BinaryUpload( fileManagement, this );
            _download = new BinaryDownload( fileManagement );
            _fileManagement = fileManagement;
            _poolDispatcher = new ServerPoolDispatcher( _upload, _download, this );
            _serverProvider = serverProvider;
            _poolConnectionFactory = serverPoolConnectionFactory;
            _configurationName = startupInfo.ConfigName;
            _requestPipeline = new RequestPipeline( this, interceptors, configurationStore, _configurationName );


            var configuration = configurationStore.Read<ServerConfiguration>( _configurationName );
            var port = (startupInfo.Port ?? configuration?.Port).GetValueOrDefault( ServerStartInformation.DefaultPort );
            startupInfo.Port = port;
            EnsurePortIsAvailable( port );

            var uuid = startupInfo.ServerUuid;
            if(uuid == Guid.Empty)
            {
                if(!(configuration?.Identifier != null && Guid.TryParse( configuration.Identifier, out uuid ) && uuid != Guid.Empty))
                {
                    uuid = Guid.NewGuid();
                }
                startupInfo.ServerUuid = uuid;
            }

            _logger.Info( $"Starting server {uuid} on port {port}." );

            ServerType = startupInfo.Info.Type;
            ServerDescription = startupInfo.Info.Description;
            ServerVendorURL = startupInfo.Info.VendorUri;
            ServerVersion = startupInfo.Info.Version;
            ServerUUID = uuid.ToString();
            ServerName = startupInfo.Name ?? configuration?.Name ?? ServerType;
            _implementedFeatures = new List<IFeatureProvider>();
            _loggingInterceptor = new LoggingInterceptor( _logger );

            var networkFilterString = startupInfo.NetworkInterfaces ?? configuration?.InterfaceFilter;

            // create gRPC server such that it uses only the specified network interfaces
            if (networkFilterString != null && networkFilterString != "none")
            {
                var filter = Networking.CreateNetworkInterfaceFilter( networkFilterString );

                _announcer = new ServiceAnnouncer( ServerUUID, (ushort)port, filter );

                _serverProvider.ConfigureForServer( uuid, port, filter );

                if(_serverProvider.Credentials is SslServerCredentials secureCredentials)
                {
                    var lines = secureCredentials.RootCertificates.Split( new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
                    for(int i = 0; i < lines.Length; i++)
                    {
                        _announcer.SetProperty( "ca" + i, lines[i] );
                    }
                }
                else
                {
                    _announcer.SetProperty( "encrypted", "false" );
                }
            }
            else
            {
                _logger.Warn( "Server is running without Discovery!" );
                // if no interface was specified, the SiLA server will (try to) run on all of them
                _serverProvider.ConfigureForServer( uuid, port, null );
            }

            _serverProvider.AddService( _upload.CreateServiceDefinition() );
            _serverProvider.AddService( _download.CreateServiceDefinition() );

            var cleanupInterval = TimeSpan.FromSeconds( 30 );
            _cleanupTimer = new Timer( Cleanup, null, cleanupInterval, cleanupInterval );

            if(configuration == null)
            {
                TryPersistConfiguration( startupInfo, configurationStore, port );
            }
        }


        /// <summary>
        /// Adds the given feature to the server
        /// </summary>
        /// <param name="feature">The feature to expose</param>
        public void AddFeature( IFeatureProvider feature )
        {
            _implementedFeatures.Add( feature );
            var definitionBuilder = new ServiceDefinitionBuilder();
            var builder = new ServerBuilder( this, feature.FeatureDefinition, _poolDispatcher, definitionBuilder );
            feature.Register( builder );
            _serverProvider.AddService( definitionBuilder.Build().Intercept( _loggingInterceptor ) );
            _logger.Info( $"Exposing feature {feature.FeatureDefinition.FullyQualifiedIdentifier}" );

            if (feature is ConnectionConfigurationServiceProvider connectionConfigurationServiceProvider)
            {
                _clientConnectionStatus = connectionConfigurationServiceProvider.ConnectionConfigurationStatus;
            }
        }

    }
}

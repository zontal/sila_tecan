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
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.Server.Binary;
using Tecan.Sila2.Server.ServerPooling;
using Tecan.Sila2.Server.ServiceDefinition;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server
{
    public partial class SiLAServer
    {
        #region Members

        private readonly SilaServiceBinder _serviceBinder;

        #endregion

        #region Constructors and destructors

        /// <summary>
        /// Creates a new instance of the SiLA2Server class, adds the SiLAService Feature implementation, creates a gRPC Server and registers the server for Server discovery.
        /// </summary>
        /// <param name="startupInfo">The information the server requires to start up</param>
        /// <param name="configurationStore">An object to read and write server configuration</param>
        /// <param name="interceptors">Interceptors to intercept any commands issued to the server</param>
        /// <param name="fileManagement">The file management used by this server</param>
        /// <param name="serviceHandlerRepository">A repository of service handlers</param>
        /// <param name="serverPoolConnectionFactory">A component that creates a callinvoker for a request to connect to a pool server</param>
        [ImportingConstructor]
        public SiLAServer( ServerStartInformation startupInfo, IConfigurationStore configurationStore, [ImportMany] IEnumerable<IRequestInterceptor> interceptors, IFileManagement fileManagement, IServiceConfigurationBuilder<ISiLAServer> serviceHandlerRepository, IServerPoolConnectionFactory serverPoolConnectionFactory )
        {
            _configurationStore = configurationStore;
            _upload = new BinaryUpload( fileManagement, this );
            _download = new BinaryDownload( fileManagement );
            _fileManagement = fileManagement;
            _poolDispatcher = new ServerPoolDispatcher( _upload, _download, this );
            _poolConnectionFactory = serverPoolConnectionFactory;
            _serviceBinder = new SilaServiceBinder( serviceHandlerRepository );
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

            serviceHandlerRepository.AddServiceHandler( _download );
            serviceHandlerRepository.AddServiceHandler( _upload );

            if((startupInfo.NetworkInterfaces ?? configuration?.InterfaceFilter) != null)
            {
                var filter = Networking.CreateNetworkInterfaceFilter( startupInfo.NetworkInterfaces ?? configuration?.InterfaceFilter );

                _announcer = new ServiceAnnouncer( ServerUUID, (ushort)port, filter );

                serviceHandlerRepository.ConfigureForServer( uuid, port, filter );
            }
            else
            {
                _logger.Warn( "Server is running without Discovery!" );
                // if no interface was specified, the SiLA server will (try to) run on all of them
                serviceHandlerRepository.ConfigureForServer( uuid, port, null );
            }

            if(serviceHandlerRepository.AnnouncementDetails != null && _announcer != null)
            {
                foreach(var pair in serviceHandlerRepository.AnnouncementDetails)
                {
                    _announcer.SetProperty( pair.Key, pair.Value );
                }
            }

            var cleanupInterval = TimeSpan.FromSeconds( 30 );
            _cleanupTimer = new Timer( Cleanup, null, cleanupInterval, cleanupInterval );

            if(configuration == null)
            {
                TryPersistConfiguration( startupInfo, configurationStore, port );
            }
        }

        #endregion

        #region Feature discovery

        /// <summary>
        /// Adds the given feature to the server
        /// </summary>
        /// <param name="feature">The feature to expose</param>
        public void AddFeature( IFeatureProvider feature )
        {
            _implementedFeatures.Add( feature );
            var builder = new ServerBuilder( this, feature.FeatureDefinition, _poolDispatcher, _serviceBinder );
            feature.Register( builder );
            _logger.Info( $"Exposing feature {feature.FeatureDefinition.FullyQualifiedIdentifier}" );
        }

        #endregion
    }
}

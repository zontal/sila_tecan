using Grpc.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Security;
using GrpcServer = Grpc.Core.Server;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a component that provides a gRPC server
    /// </summary>
    [Export(typeof(IServerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ServerProvider : IServerProvider
    {
        private readonly ILoggingChannel _logger;
        private readonly GrpcServer _server = new GrpcServer();
        private readonly IServerCertificateProvider _certificateProvider;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="loggingChannelFactory">the logging channel factory</param>
        /// <param name="certificateProvider">the certificate provider</param>
        [ImportingConstructor]
        public ServerProvider(ILoggingChannelFactory loggingChannelFactory, IServerCertificateProvider certificateProvider)
        {
            _logger = loggingChannelFactory.CreateChannel( typeof( ServerProvider ) );
            _certificateProvider = certificateProvider;
        }

        /// <inheritdoc />
        public ServerCredentials Credentials { get; set; }

        /// <inheritdoc />
        public void AddService( ServerServiceDefinition serviceDefinition )
        {
            _server.Services.Add( serviceDefinition );
        }

        /// <inheritdoc />
        public void Configure( string host, int port )
        {
            if(Credentials == null)
            {
                var context = _certificateProvider.CreateContext();
                if(context is PemCertificateContext pemContext)
                {
                    var keypair = new KeyCertificatePair( pemContext.Certificate, pemContext.Key );
                    _logger.Log( "Found certificates, starting server with TLS.".ToString, Severity.Info );
                    Credentials = new SslServerCredentials( new List<KeyCertificatePair>() { keypair }, pemContext.CA, SslClientCertificateRequestType.DontRequest );
                }
                else
                {
                    _logger.Log( $"No certificates found. Server will start with plain-text communication".ToString, Severity.Warning );
                    Credentials = ServerCredentials.Insecure;
                }
            }

            _server.Ports.Add( new ServerPort( host, port, Credentials ) );
        }

        /// <inheritdoc />
        public void ConfigureForServer( Guid guid, int port, Predicate<NetworkInterface> networkInterfaceFilter )
        {
            if(Credentials == null)
            {
                var context = _certificateProvider.CreateContext( guid );
                if(context is PemCertificateContext pemContext)
                {
                    var keypair = new KeyCertificatePair( pemContext.Certificate, pemContext.Key );
                    _logger.Log( "Found certificates, starting server with TLS.".ToString, Severity.Info );
                    Credentials = new SslServerCredentials( new List<KeyCertificatePair>() { keypair }, pemContext.CA, SslClientCertificateRequestType.DontRequest );
                }
                else
                {
                    _logger.Log( $"No certificates found. Server will start with plain-text communication".ToString, Severity.Warning );
                    Credentials = ServerCredentials.Insecure;
                }
            }

            if(networkInterfaceFilter != null)
            {
                _logger.Log( "Running on the following addresses:".ToString, Severity.Info );
                foreach(var v4addr in NetworkInterface.GetAllNetworkInterfaces()
                    .Where( nic => networkInterfaceFilter( nic ) )
                    .SelectMany( nic => nic.GetIPProperties().UnicastAddresses.Select( ua => ua.Address ) )
                    .Where( a => a.AddressFamily == AddressFamily.InterNetwork ))
                {
                    _logger.Log( v4addr.ToString, Severity.Info );
                    _server.Ports.Add( new ServerPort( v4addr.ToString(), port, Credentials ) );
                }
            }
            else
            {
                // if no interface was specified, the SiLA server will (try to) run on all of them
                _server.Ports.Add( new ServerPort( "0.0.0.0", port, Credentials ) );
            }
        }

        /// <inheritdoc />
        public Task ShutdownAsync()
        {
            _logger.Log( "Shutting down gRPC server".ToString, Severity.Info );
            return _server.ShutdownAsync();
        }

        /// <inheritdoc />
        public void Start()
        {
            _logger.Log( "Starting gRPC server".ToString, Severity.Info );
            _server.Start();
        }
    }
}

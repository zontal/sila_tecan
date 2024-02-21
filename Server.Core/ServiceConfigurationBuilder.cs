using Common.Logging;
using Grpc.AspNetCore.Server.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Tecan.Sila2.Security;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes a service configuration builder for Kestrel
    /// </summary>
    public class ServiceConfigurationBuilder : IServiceConfigurationBuilder<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>>
    {
        private readonly List<IServiceHandler<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>>> _handlers = new List<IServiceHandler<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>>>();
        private readonly Dictionary<IPAddress, int> _networkAddresses = new Dictionary<IPAddress, int>();
        private readonly IServerCertificateProvider _certificateProvider;
        private readonly ILog _loggingChannel = LogManager.GetLogger<ServiceConfigurationBuilder>();
        private readonly Dictionary<string, string> _announcementDetails = new Dictionary<string, string>();

        /// <inheritdoc />
        public IEnumerable<IServiceHandler<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>>> ServiceHandlers => _handlers;
        private X509Certificate2 _certificate;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="certificateProvider">A server certificate provider</param>
        public ServiceConfigurationBuilder( IServerCertificateProvider certificateProvider )
        {
            _certificateProvider = certificateProvider;
        }

        /// <summary>
        /// Gets a dictionary of network interfaces that the server should be running on
        /// </summary>
        public IReadOnlyDictionary<IPAddress, int> NetworkAddresses => _networkAddresses;

        /// <inheritdoc />
        public void AddServiceHandler( IServiceHandler<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>> serviceHandler )
        {
            _handlers.Add( serviceHandler );
        }

        /// <inheritdoc />
        public void Configure( string host, int port )
        {
            if(_certificate == null)
            {
                UseContext( _certificateProvider.CreateContext(), null );
            }

            if(IPAddress.TryParse( host, out var addr ))
            {
                _networkAddresses.Add( addr, port );
            }
        }

        private bool IsJustAny => _networkAddresses.Count == 1 && _networkAddresses.ContainsKey( IPAddress.Any );

        private int AnyPort => _networkAddresses[IPAddress.Any];

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> AnnouncementDetails => _announcementDetails;

        /// <inheritdoc />
        public void ConfigureForServer( Guid guid, int port, Predicate<NetworkInterface> networkInterfaceFilter )
        {
            if(_certificate == null)
            {
                var context = _certificateProvider.CreateContext( guid );
                UseContext( context, guid );
            }

            if(networkInterfaceFilter != null)
            {
                _loggingChannel.Info( "Running on the following addresses:" );
                foreach(var v4addr in NetworkInterface.GetAllNetworkInterfaces()
                    .Where( nic => networkInterfaceFilter( nic ) )
                    .SelectMany( nic => nic.GetIPProperties().UnicastAddresses.Select( ua => ua.Address ) )
                    .Where( a => a.AddressFamily == AddressFamily.InterNetwork ))
                {
                    _loggingChannel.Info( v4addr.ToString() );
                    _networkAddresses.Add( v4addr, port );
                }
            }
            else
            {
                _networkAddresses.Add( IPAddress.Any, port );
            }
        }

        private void UseContext(CertificateContext context, Guid? serverUuid)
        {
            if (context != null)
            {
                _loggingChannel.Info("Found certificates, starting server with TLS.");
                _certificate = CertificateGenerator.GetServerCertificate(context, serverUuid, out var ca);

                if (_certificate != null)
                {
                    var certLines = Convert.ToBase64String((ca ?? _certificate).Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks).Split('\n');
                    _announcementDetails.Add("ca0", "-----BEGIN CERTIFICATE-----");
                    for (int i = 0; i < certLines.Length; i++)
                    {
                        _announcementDetails.Add("ca" + (i + 1), certLines[i].TrimEnd('\r'));
                    }
                    _announcementDetails.Add("ca" + (certLines.Length + 1), "-----END CERTIFICATE-----");
                    return;
                }
            }
            _loggingChannel.Warn($"No certificates found. Server will start with plain-text communication");
            _certificate = null;
            _announcementDetails.Add("encrypted", "false");
        }

        /// <summary>
        /// Configures the provided Kestrel server
        /// </summary>
        /// <param name="options">The Kestrel configuration</param>
        public void ConfigureKestrel( KestrelServerOptions options )
        {
            ConfigureKestrel( options, HttpProtocols.Http2 );
        }
        /// <summary>
        /// Configures the provided Kestrel server
        /// </summary>
        /// <param name="options">The Kestrel configuration</param>
        /// <param name="protocols">The protocols to connect</param>
        public void ConfigureKestrel( KestrelServerOptions options, HttpProtocols protocols )
        {
            Action<ListenOptions> listenOptions = ( ListenOptions opts ) =>
                {
                    opts.Protocols = protocols;
                    if(_certificate != null)
                    {
                        if(_certificate != null)
                        {
                            opts.UseHttps( _certificate );
                        }
                    }
                };
            if(!IsJustAny)
            {
                foreach(var networkInterface in NetworkAddresses)
                {
                    options.Listen( networkInterface.Key, networkInterface.Value, listenOptions );
                }
            }
            else
            {
                options.ListenAnyIP( AnyPort, listenOptions );
            }
        }
    }
}

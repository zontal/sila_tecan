using Common.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Tecan.Sila2.Security;

namespace Tecan.Sila2.Server
{
    internal class ServiceHandlerRepository : IServiceHandlerRepository<ISiLAServer>
    {
        private readonly List<IServiceHandler<ISiLAServer>> _handlers = new List<IServiceHandler<ISiLAServer>>();
        private readonly Dictionary<IPAddress, int> _networkAddresses = new Dictionary<IPAddress, int>();
        private readonly IServerCertificateProvider _certificateProvider;
        private readonly ILog _loggingChannel = LogManager.GetLogger<ServiceHandlerRepository>();

        public IEnumerable<IServiceHandler<ISiLAServer>> ServiceHandlers => _handlers;
        private PfxCertificateContext _certificateContext;

        public ServiceHandlerRepository(IServerCertificateProvider certificateProvider)
        {
            _certificateProvider = certificateProvider;
        }

        public IReadOnlyDictionary<IPAddress, int> NetworkAddresses => _networkAddresses;

        public string PathToPfx => _certificateContext?.PathToPfx;

        public void AddServiceHandler( IServiceHandler<ISiLAServer> serviceHandler )
        {
            _handlers.Add( serviceHandler );
        }

        public void Configure( string host, int port )
        {
            if(_certificateContext == null)
            {
                var context = _certificateProvider.CreateContext();
                if(context is PfxCertificateContext pfxContext)
                {
                    _loggingChannel.Info( "Found certificates, starting server with TLS." );
                    _certificateContext = pfxContext;
                }
                else
                {
                    _loggingChannel.Warn( $"No certificates found. Server will start with plain-text communication" );
                    _certificateContext = null;
                }
            }

            if (IPAddress.TryParse(host, out var addr))
            {
                _networkAddresses.Add( addr, port );
            }
        }

        private bool IsJustAny => _networkAddresses.Count == 1 && _networkAddresses.ContainsKey( IPAddress.Any );

        private int AnyPort => _networkAddresses[IPAddress.Any];

        public void ConfigureForServer( Guid guid, int port, Predicate<NetworkInterface> networkInterfaceFilter )
        {
            if(_certificateContext == null)
            {
                var context = _certificateProvider.CreateContext( guid );
                if(context is PfxCertificateContext pfxContext)
                {
                    _loggingChannel.Info( "Found certificates, starting server with TLS." );
                    _certificateContext = pfxContext;
                }
                else
                {
                    _loggingChannel.Warn( $"No certificates found. Server will start with plain-text communication" );
                    _certificateContext = null;
                }
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

        public void ConfigureKestrel(KestrelServerOptions options)
        {
            Action<ListenOptions> listenOptions = ( ListenOptions opts ) =>
                {
                    opts.Protocols = HttpProtocols.Http2;
                    if(_certificateContext != null)
                    {
                        if(_certificateContext.PathToPfx == null)
                        {
                            opts.UseHttps();
                        }
                        else
                        {
                            opts.UseHttps( _certificateContext.PathToPfx );
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

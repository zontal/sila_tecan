using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Makaretu.Dns;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Discovery
{
    /// <summary>
    /// Utility class to be used to discover and find SiLA Servers using the 
    /// ServiceFinder class.
    /// </summary>
    public class ServerDiscovery : IServerDiscovery
    {
        private readonly ILoggingChannel _loggingChannel;
        private readonly Networking _networking;
        private readonly IServerConnector _serverConnector;

        /// <summary>
        /// Create a new sila discovery object
        /// </summary>
        /// <param name="factory">A logging factory</param>
        /// <param name="serverConnector">A component that creates channels</param>
        public ServerDiscovery( ILoggingChannelFactory factory, IServerConnector serverConnector )
        {
            _loggingChannel = factory.CreateChannel( typeof(ServerDiscovery) );
            _networking = new Networking( factory );
            _serverConnector = serverConnector;
        }

        /// <inheritdoc />
        public ServerData Connect( Guid serverUuid, TimeSpan timeout, CancellationToken cancellationToken = default )
        {
            ServerData connection = null;
            using( var cts = new CancellationTokenSource() )
            {
                if( cancellationToken.CanBeCanceled )
                {
                    cancellationToken.Register( cts.Cancel );
                }

                DiscoverServers( channel =>
                {
                    if( channel.Config.Uuid == serverUuid )
                    {
                        connection = channel;
                        cts.Cancel();
                    }
                }, timeout, cts.Token);
            }

            return connection;
        }

        public void DiscoverServers( Action<ServerData> serverFoundCallback, TimeSpan timeout = default, CancellationToken cancellationToken = default )
        {
            using( var mdns = new MulticastService( _networking.CreateFilterFunc() ) )
            {
                var handler = new EventHandler<MessageEventArgs>( ( s, e ) =>
                {
                    // The first Answer will be a PTRRecord
                    var pointers = e.Message.Answers.OfType<PTRRecord>().Where( p => p.Name.ToString().Contains( DiscoveryConstants.ServiceName ) );
                    foreach( var pointer in pointers )
                    {
                        // Ask for the service instance details
                        mdns.SendQuery( pointer.DomainName, type: DnsType.SRV );
                    }

                    // The second Answer will be a SRVRecord (also contain A and AAAA records)
                    var servers = e.Message.Answers.OfType<SRVRecord>().Where( p => p.Name.ToString().Contains( DiscoveryConstants.ServiceName ) );
                    foreach( var server in servers )
                    {
                        var aRecord = e.Message.AdditionalRecords.OfType<ARecord>().FirstOrDefault();
                        if( aRecord != null )
                        {
                            try
                            {
                                var channel = _serverConnector.Connect( aRecord.Address, server.Port );
                                serverFoundCallback( channel );
                            }
                            catch( Exception exception )
                            {
                                _loggingChannel.LogException( $"Failed to create channel to {aRecord.Address} on port {server.Port}".ToString, exception );
                            }
                        }
                    }
                } );

                mdns.AnswerReceived += handler;

                mdns.Start();
                // Ask for PTRRecords (the devices will only respond to SRV that are directed exactly to them)
                mdns.SendQuery( string.Join( ".", DiscoveryConstants.ServiceName, DiscoveryConstants.ServiceDomainName ), type: DnsType.PTR );
                if( cancellationToken.CanBeCanceled )
                {
                    if( timeout > TimeSpan.Zero )
                    {
                        cancellationToken.WaitHandle.WaitOne( timeout );
                    }
                    else
                    {
                        cancellationToken.WaitHandle.WaitOne();
                    }
                }
                else
                {
                    Thread.Sleep( timeout );
                }

                mdns.Stop();
                mdns.AnswerReceived -= handler;
            }
        }

        /// <summary>
        /// Query a list of SiLA2 servers with valid SiLAService
        /// </summary>
        /// <param name="timeout">duration of Query period</param>
        /// <returns>"List of valid SiLA2 servers</returns>
        public IEnumerable<ServerData> GetServers(TimeSpan timeout)
        {
            var services = new List<ServerData>();
            DiscoverServers(services.Add, timeout);
            return services;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Makaretu.Dns;
using Tecan.MAP.Contracts.Services.Logging;

namespace Tecan.Sila2.Discovery
{
    /// <summary>
    /// Utility class to be used to discover and find SiLA Servers using the 
    /// ServiceFinder class.
    /// </summary>
    [Export(typeof(IServerDiscovery))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ServerDiscovery : IServerDiscovery
    {
        private readonly ILoggingChannel _loggingChannel;
        private readonly IServerConnector _serverConnector;
        private readonly Networking _networking;

        private const int GuidLength = 36;

        private readonly Dictionary<string, ServerData> _connectedServers = new Dictionary<string, ServerData>();

        /// <summary>
        /// Create a new sila discovery object
        /// </summary>
        /// <param name="factory">A logging factory</param>
        /// <param name="serverConnector">A component that creates channels</param>
        [ImportingConstructor]
        public ServerDiscovery( ILoggingChannelFactory factory, IServerConnector serverConnector )
        {
            _loggingChannel = factory.CreateChannel( typeof(ServerDiscovery) );
            _serverConnector = serverConnector;
            _networking = new Networking( factory );
        }

        /// <inheritdoc />
        public ServerData Connect( Guid serverUuid, TimeSpan timeout, Predicate<NetworkInterface> networkFilter = null, CancellationToken cancellationToken = default )
        {
            var cts = new TaskCompletionSource<ServerData>();
            cancellationToken.Register( cts.SetCanceled );
            var found = false;

            DiscoverServers( channel =>
            {
                if( channel.Config.Uuid == serverUuid )
                {
                    cts.SetResult(channel);
                    found = true;
                }
            }, timeout, networkFilter, cancellationToken );

            if (!found)
            {
                cts.TrySetResult( null );
            }

            return cts.Task.Result;
        }

        /// <inheritdoc />
        public void DiscoverServers( Action<ServerData> serverFoundCallback, TimeSpan timeout = default, Predicate<NetworkInterface> networkInterfaceFilter = null, CancellationToken cancellationToken = default )
        {
            if (serverFoundCallback == null)
            {
                throw new ArgumentNullException( nameof( serverFoundCallback ) );
            }

            lock(_connectedServers)
            {
                NotifyKnownServers( serverFoundCallback );
            }
            using(var mdns = new MulticastService( _networking.CreateFilter( networkInterfaceFilter ) ))
            {
                var handler = new EventHandler<MessageEventArgs>( ( s, e ) =>
                {
                    // The first Answer will be a PTRRecord, in that case request an SRV if it is not already contained in the answer
                    RequestSrvForPtrRecord( e.Message, mdns );

                    // The second Answer will be a SRVRecord (also contain A and AAAA records or as separate answers)
                    var servers = e.Message.Answers.OfType<SRVRecord>().Where( p => p.Name.ToString().Contains( DiscoveryConstants.ServiceName ) );
                    foreach(var server in servers)
                    {
                        var aRecord = e.Message.AdditionalRecords.OfType<ARecord>().FirstOrDefault() ?? e.Message.Answers.OfType<ARecord>().FirstOrDefault();
                        lock(_connectedServers)
                        {
                            var serverName = server.Name.ToString();
                            if(aRecord != null && !_connectedServers.ContainsKey( serverName ))
                            {
                                // see whether there are TXT records
                                var details = ReadDetails( e.Message.Answers.Concat( e.Message.AdditionalRecords ) );
                                try
                                {
                                    var channel = _serverConnector.Connect( aRecord.Address, server.Port, Guid.TryParse( serverName.Substring( 0, GuidLength ), out var serverUuid ) ? serverUuid : null, details );
                                    _connectedServers[serverName] = channel;
                                    serverFoundCallback( channel );
                                }
                                catch(Exception exception)
                                {
                                    _loggingChannel.LogException( $"Failed to create channel to {aRecord.Address} on port {server.Port}".ToString, exception );
                                }
                            }
                        }
                    }
                } );

                mdns.AnswerReceived += handler;

                mdns.Start();
                ResendQueryTillTimeoutOrCancel( timeout, mdns, cancellationToken );

                mdns.Stop();
                mdns.AnswerReceived -= handler;
            }
        }

        private IReadOnlyDictionary<string, string> ReadDetails( IEnumerable<ResourceRecord> records )
        {
            Dictionary<string, string> result = null;
            foreach(var item in records.OfType<TXTRecord>().SelectMany(txt => txt.Strings))
            {
                if (!string.IsNullOrEmpty(item))
                {
                    var equalsPos = item.IndexOf( '=' );
                    if (equalsPos > 0)
                    {
                        if (result == null)
                        {
                            result = new Dictionary<string, string>();
                        }
                        result[item.Substring( 0, equalsPos )] = item.Substring( equalsPos + 1 );
                    }
                }
            }
            return result;
        }

        private void NotifyKnownServers( Action<ServerData> serverFoundCallback )
        {
            foreach(var knownServer in _connectedServers.Keys.ToArray())
            {
                var server = _connectedServers[knownServer];
                if(server.Channel.State == Client.ChannelState.Shutdown)
                {
                    _loggingChannel.Log( $"Removing server {server.Config.Name} because the connection is lost".ToString, Severity.Info );
                    _connectedServers.Remove( knownServer );
                }
                else
                {
                    serverFoundCallback( server );
                }
            }
        }

        private void RequestSrvForPtrRecord( Message message, MulticastService mdns )
        {
            var pointers = message.Answers.OfType<PTRRecord>().Where( p => p.Name.ToString().Contains( DiscoveryConstants.ServiceName ) );
            foreach(var pointer in pointers)
            {
                if(!_connectedServers.ContainsKey( pointer.DomainName.ToString() ) && !message.Answers.OfType<SRVRecord>().Any() )
                {
                    // Ask for the service instance details
                    mdns.SendQuery( pointer.DomainName, type: DnsType.SRV );
                }
            }
        }

        private static void ResendQueryTillTimeoutOrCancel( TimeSpan timeout, MulticastService mdns, CancellationToken cancellationToken )
        {
            var toWait = TimeSpan.FromSeconds( 1 );
            while(timeout > TimeSpan.Zero)
            {
                // Ask for PTRRecords (the devices will only respond to SRV that are directed exactly to them)
                mdns.SendQuery( DiscoveryConstants.ServiceName + "." + DiscoveryConstants.ServiceDomainName + ".", DnsClass.IN, DnsType.PTR );
                Wait( new TimeSpan(Math.Min(toWait.Ticks, timeout.Ticks)), cancellationToken );
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                else
                {
                    timeout -= toWait;
                    toWait += toWait;
                }
            }
        }

        private static void Wait( TimeSpan timeout, CancellationToken cancellationToken )
        {
            if(cancellationToken.CanBeCanceled)
            {
                if(timeout > TimeSpan.Zero)
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
        }

        /// <inheritdoc />
        public IEnumerable<ServerData> GetServers(TimeSpan timeout, Predicate<NetworkInterface> networkFilter = null )
        {
            var services = new List<ServerData>();
            DiscoverServers(services.Add, timeout, networkFilter);
            return services;
        }
    }
}

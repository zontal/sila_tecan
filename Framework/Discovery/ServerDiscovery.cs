using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
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
        public ServerDiscovery(ILoggingChannelFactory factory, IServerConnector serverConnector)
        {
            _loggingChannel = factory.CreateChannel(typeof(ServerDiscovery));
            _networking = new Networking(factory);
            _serverConnector = serverConnector;
        }
        
        /// <summary>
        /// Query a list of SiLA2 servers with valid SiLAService
        /// </summary>
        /// <param name="timeout">duration of Query period</param>
        /// <returns>"List of valid SiLA2 servers</returns>
        public IEnumerable<ServerData> GetServers(TimeSpan timeout)
        {

            var services = new List<ServerData>();

            using(var mdns = new MulticastService( _networking.CreateFilterFunc() ))
            {
                // attach the event handler for processing responses.
                mdns.AnswerReceived += ( s, e ) =>
                {
                    // The first Answer will be a PTRRecord
                    var pointers = e.Message.Answers.OfType<PTRRecord>().Where( p => p.Name.ToString().Contains( DiscoveryConstants.ServiceName ) );
                    foreach(var pointer in pointers)
                    {
                        // Ask for the service instance details
                        mdns.SendQuery( pointer.DomainName, type: DnsType.SRV );
                    }

                    // The second Answer will be a SRVRecord (also contain A and AAAA records)
                    var servers = e.Message.Answers.OfType<SRVRecord>().Where( p => p.Name.ToString().Contains( DiscoveryConstants.ServiceName ) );
                    foreach(var server in servers)
                    {
                        var aRecord = e.Message.AdditionalRecords.OfType<ARecord>().FirstOrDefault();
                        if(aRecord != null)
                        {
                            try
                            {
                                var channel = _serverConnector.Connect( aRecord.Address, server.Port );
                                services.Add( channel );
                            }
                            catch(Exception exception)
                            {
                                _loggingChannel.LogException( $"Failed to create channel to {aRecord.Address} on port {server.Port}".ToString, exception );
                            }
                        }
                    }
                };

                mdns.Start();
                // Ask for PTRRecords (the devices will only respond to SRV that are directed exactly to them)
                mdns.SendQuery( string.Join( ".", DiscoveryConstants.ServiceName, DiscoveryConstants.ServiceDomainName ), type: DnsType.PTR );
                Thread.Sleep( timeout );
                mdns.Stop();
            }

            return services;
        }
    }
}

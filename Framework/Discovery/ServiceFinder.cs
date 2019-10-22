using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Grpc.Core;
using Makaretu.Dns;
using Tecan.MAP.Contracts.Services.Logging;

namespace Tecan.Sila2.Discovery
{
    /// <summary>
    /// Class to query for sila services via mDNS queries.
    /// </summary>
    internal class ServiceFinder
    {
        private readonly ILoggingChannel _log;
        private readonly Networking _networking;

        /// <summary>
        /// Create a new service finder
        /// </summary>
        /// <param name="loggingChannelFactory">The logging factory</param>
        /// <param name="networking">The networking object</param>
        public ServiceFinder(ILoggingChannelFactory loggingChannelFactory, Networking networking)
        {
            _log = loggingChannelFactory.CreateChannel(typeof(ServiceFinder));
            _networking = networking;
        }

        /// <summary>
        /// Queries for a service of the given type thats name and host name contain the given patterns and returns the all matching services unless the given time exceeds.
        /// </summary>
        /// <param name="timeout">Max time (in milli-seconds) to discover </param>
        /// <returns>The list of services matching the given conditions or and empty list if nothing has been discovered within the given time.</returns>
        public List<Channel> QueryAllServices(TimeSpan timeout)
        {
            var services = new List<Channel>();

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
                            services.Add( BuildNewChannel( aRecord.Address, server.Port ) );
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

        /// <summary>
        /// Utility function to create gRPC channel from IP and port from ServerDiscoveryInfo
        /// </summary>
        /// <param name="host">The hostname of the server to connect</param>
        /// <param name="port">The port on which to connect to the server</param>
        /// <returns>A channel</returns>
        public Channel BuildNewChannel(IPAddress host, int port)
        {
            var channel = new Channel($"{host}:{port}", ChannelCredentials.Insecure);
            return channel;
        }
    }
}

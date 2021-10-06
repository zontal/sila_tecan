using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Discovery
{
    /// <summary>
    /// Standard implementation of a channel creator that creates insecure channels
    /// </summary>
    public class ServerConnector : IServerConnector
    {
        private readonly IClientExecutionManager _executionManager;
        private readonly ILoggingChannel _loggingChannel;

        private const string BeginCertPhrase = "-----BEGIN CERTIFICATE-----";
        private const string EndCertPhrase = "-----END CERTIFICATE-----";

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="executionManager">A client execution manager</param>
        /// <param name="loggingChannelFactory">A factory used for logging</param>
        public ServerConnector( IClientExecutionManager executionManager, ILoggingChannelFactory loggingChannelFactory )
        {
            _executionManager = executionManager ?? new DiscoveryExecutionManager();
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof(ServerConnector) );
        }

        /// <inheritdoc />
        public virtual ServerData Connect( IPAddress host, int port )
        {
            return Connect( $"{host}:{port}", null, null );
        }

        private ServerData Connect(string target, Guid? expectedGuid, IReadOnlyDictionary<string, string> details)
        {
            var connection = ConnectCore( target, expectedGuid, details );
            if (expectedGuid.HasValue && connection != null && connection.Config.Uuid != expectedGuid.Value)
            {
                _loggingChannel.Log( $"Connection to {target} was successful, but the server UUID {connection.Config.Uuid} does not match the expected {expectedGuid.Value}.".ToString, Severity.Warning );
                connection.Channel.Dispose();
                return null;
            }
            return connection;
        }

        private ServerData ConnectCore( string target, Guid? expectedGuid, IReadOnlyDictionary<string, string> details )
        {
            _loggingChannel.Log( $"Connecting to {target}".ToString, Severity.Info );
            if(details == null || !details.TryGetValue( "encrypted", out var encryption ) || encryption != "false")
            {
                string certificate = null;
                if(details != null && details.ContainsKey( "ca0" ))
                {
                    try
                    {
                        var ca = RestoreCa( details );
                        var channel = GrpcChannel.ForAddress( target, new GrpcChannelOptions
                        {

                        } );
                        var server = GetServerData( channel );
                        return CheckExpectedGuid( CheckCertificate( server, ExtractCertificate(certificate) ), expectedGuid);
                    }
                    catch(Exception e)
                    {
                        _loggingChannel.LogException(
                            $"Secure connection to {target} failed, falling back to unsecure connection".ToString, e );
                    }
                }
                else
                {
                    try
                    {
                        var channel = GrpcChannel.ForAddress( target );
                        return CheckExpectedGuid( CheckCertificate( GetServerData( channel ), ExtractCertificate( certificate ) ), expectedGuid );
                    }
                    catch(Exception e)
                    {
                        _loggingChannel.LogException(
                            $"Secure connection to {target} failed, falling back to unsecure connection".ToString, e );
                    }
                }
            }
            return GetServerData( GrpcChannel.ForAddress(target, new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Insecure
            } ));
        }

        private ServerData CheckCertificate( ServerData serverData, X509Certificate2 certificate )
        {
            if (certificate == null)
            {
                return ServerCertificateCouldNotBeRead( serverData );
            }
            Guid? expectedGuid = FindServerUuidInCertificate( certificate );
            if(expectedGuid.HasValue)
            {
                if(expectedGuid.Value == serverData.Config.Uuid)
                {
                    return serverData;
                }
                else
                {
                    return ServerUuidDoesNotMatchCertificate( serverData, expectedGuid.Value, certificate );
                }
            }
            else
            {
                return ServerUuidNotContainedInCertificate( serverData, certificate );
            }
        }

        private static Guid? FindServerUuidInCertificate( X509Certificate2 certificate )
        {
            foreach(var extension in certificate.Extensions)
            {
                if(extension.Oid.Value.ToString() == "1.3.536")
                {
                    var value = Encoding.ASCII.GetString( extension.RawData );
                    if(value.Length >= 36 && Guid.TryParse( value.Substring( value.Length - 36 ), out var serverUuid ))
                    {
                        return serverUuid;
                    }
                }
            }

            return null;
        }

        private ServerData CheckExpectedGuid(ServerData serverData, Guid? expectedGuid)
        {
            if(expectedGuid.HasValue && serverData.Config.Uuid != expectedGuid.Value)
            {
                return ServerUuidDoesNotMatchDnsEntry( serverData, expectedGuid.Value );
            }
            return serverData;
        }

        /// <summary>
        /// Gets called in the case that the server has a different UUID than what was announced during discovery
        /// </summary>
        /// <param name="server">The server that was connected</param>
        /// <param name="dnsGuid">The Server UUID found via DNS</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData ServerUuidDoesNotMatchDnsEntry(ServerData server, Guid dnsGuid)
        {
            _loggingChannel.Log( $"Server UUID does not match expected value from Discovery. Expected {dnsGuid} but the actual server id is {server.Config.Uuid}".ToString, Severity.Warning );
            return server;
        }

        /// <summary>
        /// Gets called in the case that the server has a UUID that is different than the UUID in the server certificate
        /// </summary>
        /// <param name="server">The server that was connected</param>
        /// <param name="serverIdFromCertificate">The Server UUID from the certificate</param>
        /// <param name="serverCertificate">The server certificate</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData ServerUuidDoesNotMatchCertificate(ServerData server, Guid serverIdFromCertificate, X509Certificate2 serverCertificate)
        {
            _loggingChannel.Log( $"Server UUID does not match expected value from certificate. Expected {serverIdFromCertificate} but the actual server id is {server.Config.Uuid}".ToString, Severity.Warning );
            return server;
        }

        /// <summary>
        /// Gets called in the case that the server has a certificate without a SAN matching to a server UUID
        /// </summary>
        /// <param name="server">The server that was connected</param>
        /// <param name="serverCertificate">The server certificate</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData ServerUuidNotContainedInCertificate(ServerData server, X509Certificate2 serverCertificate)
        {
            _loggingChannel.Log( $"No server uuid could be found in the certificate for server {server.Config.Uuid}".ToString, Severity.Warning );
            return server;
        }

        /// <summary>
        /// Gets called in the case that the server certificate could not be read
        /// </summary>
        /// <param name="server">The connected server</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData ServerCertificateCouldNotBeRead(ServerData server)
        {
            return null;
        }

        private X509Certificate2 ExtractCertificate( string peerPem )
        {
            if (string.IsNullOrEmpty(peerPem))
            {
                return null;
            }
            try
            {
                var beginCertificate = peerPem.IndexOf( BeginCertPhrase );
                var endCertificate = peerPem.LastIndexOf( EndCertPhrase );
                var start = beginCertificate == -1 ? 0 : beginCertificate + BeginCertPhrase.Length;
                var end = endCertificate == -1 ? peerPem.Length : endCertificate;
                var completeBase64 = peerPem.Substring( start, end - start ).Replace( "\n", string.Empty );
                var rawData = Convert.FromBase64String( completeBase64 );
                return new X509Certificate2( rawData );
            }
            catch(Exception ex)
            {
                _loggingChannel.LogException( "Failed to read certificate".ToString, ex );
                return null;
            }
        }

        private string RestoreCa( IReadOnlyDictionary<string, string> details )
        {
            var sb = new StringBuilder();
            var line = 0;
            while(details.TryGetValue("ca" + line, out var caLine))
            {
                sb.AppendLine( caLine );
                line++;
            }
            return sb.ToString();
        }

        /// <inheritdoc />
        public virtual ServerData Connect( string host, int port )
        {
            return Connect( $"{host}:{port}", null, null );
        }


        /// <summary>
        /// Load all server properties 
        /// </summary>
        public ServerData GetServerData( GrpcChannel channel )
        {
            var clientChannel = new SilaChannel( channel );
            var silaService = new SiLAServiceClient( clientChannel, _executionManager );
            var config = GetServerConfig( silaService );
            var info = GetServerInfo( silaService );
            var features = GetImplementedFeatures( silaService );
            var server = new ServerData( config, info, features, clientChannel );
            return server;
        }

        private static List<Feature> GetImplementedFeatures( ISiLAService silaService )
        {
            return silaService.ImplementedFeatures.Select( f => FeatureSerializer.LoadFromXml( silaService.GetFeatureDefinition( f ) ) ).ToList();
        }

        private static ServerConfig GetServerConfig( ISiLAService silaService )
        {
            var name = silaService.ServerName;
            var uuid = Guid.Parse( silaService.ServerUUID );
            var config = new ServerConfig( name, uuid );
            return config;
        }

        private static ServerInformation GetServerInfo( ISiLAService silaService )
        {
            var type = silaService.ServerType;
            var description = silaService.ServerDescription;
            var vendorUri = silaService.ServerVendorURL;
            var version = silaService.ServerVersion;
            var info = new ServerInformation( type, description, vendorUri, version );
            return info;
        }

        /// <inheritdoc />
        public ServerData Connect( IPAddress host, int port, Guid? expectedServer, IReadOnlyDictionary<string, string> details )
        {
            return Connect( $"{host}:{port}", expectedServer, details );
        }

        /// <inheritdoc />
        public ServerData Connect( string host, int port, Guid? expectedServer, IReadOnlyDictionary<string, string> details )
        {
            return Connect( $"{host}:{port}", expectedServer, details );
        }
    }
}

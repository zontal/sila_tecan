using Common.Logging;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Tecan.Sila2.Client;
using Tecan.Sila2.Security;

namespace Tecan.Sila2.Discovery
{
    /// <summary>
    /// Standard implementation of a channel creator that creates insecure channels
    /// </summary>
    public class ServerConnector : IServerConnector
    {
        private readonly IClientExecutionManager _executionManager;
        private readonly ILog _loggingChannel = LogManager.GetLogger<ServerConnector>();

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="executionManager">A client execution manager</param>
        public ServerConnector( IClientExecutionManager executionManager )
        {
            _executionManager = executionManager ?? new DiscoveryExecutionManager();
        }

        /// <inheritdoc />
        public virtual ServerData Connect( IPAddress host, int port )
        {
            return Connect( $"{host}:{port}", null, null, true );
        }

        private ServerData Connect( string target, Guid? expectedGuid, IReadOnlyDictionary<string, string> details, bool prependScheme )
        {
            var connection = ConnectCore( target, expectedGuid, details, prependScheme );
            if(expectedGuid.HasValue && connection != null && connection.Config.Uuid != expectedGuid.Value)
            {
                _loggingChannel.Warn( $"Connection to {target} was successful, but the server UUID {connection.Config.Uuid} does not match the expected {expectedGuid.Value}." );
                connection.Channel.Dispose();
                return null;
            }
            return connection;
        }

        private ServerData ConnectCore( string target, Guid? expectedGuid, IReadOnlyDictionary<string, string> details, bool prependScheme )
        {
            _loggingChannel.Info( $"Connecting to {target}" );
            if(details == null || !details.TryGetValue( "encrypted", out var encryption ) || encryption != "false")
            {
                if(details != null && details.ContainsKey( "ca0" ))
                {
                    try
                    {
                        var discoveryCertificate = CertificateGenerator.LoadCertificateFromPem( RestoreCa( details ) );
                        try
                        {
                            return ConnectEncrypted(target, expectedGuid, prependScheme, discoveryCertificate);
                        }
                        catch (Exception e)
                        {
                            _loggingChannel.Error(
                                $"Secure connection to {target} failed, falling back to unsecure connection", e );
                            return ConnectInsecure( target, prependScheme );
                        }
                    }
                    catch(Exception e)
                    {
                        _loggingChannel.Error( "Failed to extract certificate from discovery announcement.", e );
                    }
                }
                try
                {
                    var discoveryCertificate = CertificateGenerator.LoadCertificateFromPem( RestoreCa( details ) );
                    var certificateValidator = new ServerCertificateValidator( discoveryCertificate );
                    var clientHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = certificateValidator.ValidateActualCertificate
                    };
                    var channel = GrpcChannel.ForAddress( prependScheme ? "https://" + target : target, new GrpcChannelOptions
                    {
                        HttpHandler = clientHandler,
                    } );
                    return CheckExpectedGuid( CheckCertificate( GetServerData( channel, () => certificateValidator.ServerCertificate ), certificateValidator.ServerCertificate ), expectedGuid );
                }
                catch(Exception e)
                {
                    _loggingChannel.Error(
                        $"Secure connection to {target} failed, falling back to unsecure connection", e );
                }
            }
            return ConnectInsecure( target, prependScheme );
        }

        private ServerData ConnectEncrypted(string target, Guid? expectedGuid, bool prependScheme, X509Certificate2 ca)
        {
            var certificateValidator = new ServerCertificateValidator(ca);
            var clientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = certificateValidator.ValidateActualCertificate
            };
            var channel = GrpcChannel.ForAddress(prependScheme ? "https://" + target : target, new GrpcChannelOptions
            {
                HttpHandler = clientHandler,
            });
            var server = CheckCertificateAuthority(GetServerData(channel, () => certificateValidator.ServerCertificate), certificateValidator.ServerCertificate, ca);
            if (!certificateValidator.CheckDiscoveryCertificateMatchesActualCertificate())
            {
                server = ServerDiscoveryCertificateDoesNotMatchActualCertificate(server, certificateValidator.DiscoveryCertificate, certificateValidator.ServerCertificate);
            }
            return CheckExpectedGuid(CheckCertificate(server, certificateValidator.ServerCertificate), expectedGuid);
        }

        private ServerData ConnectInsecure( string target, bool prependScheme )
        {
            return GetServerData( GrpcChannel.ForAddress( prependScheme ? "http://" + target : target, new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Insecure
            } ) );
        }

        private ServerData CheckCertificate( ServerData serverData, X509Certificate2 certificate )
        {
            if(certificate == null)
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
                if(extension.Oid.Value.ToString() == "1.3.6.1.4.1.58583")
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

        private ServerData CheckExpectedGuid( ServerData serverData, Guid? expectedGuid )
        {
            if(expectedGuid.HasValue && serverData.Config.Uuid != expectedGuid.Value)
            {
                return ServerUuidDoesNotMatchDnsEntry( serverData, expectedGuid.Value );
            }
            return serverData;
        }

        /// <summary>
        /// Gets called in the case that the server discovery certificate does not match the actual certificate used for the HTTP/2 connection
        /// </summary>
        /// <param name="server">The server</param>
        /// <param name="discoveryCertificate">The certificate announced during discovery</param>
        /// <param name="actualCertificate">The actual certificate used to authenticate the HTTP request</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData ServerDiscoveryCertificateDoesNotMatchActualCertificate(ServerData server, X509Certificate2 discoveryCertificate, X509Certificate2 actualCertificate)
        {
            _loggingChannel.Warn($"Server {server.Config.Name} uses a different certificate for discovery than for the actual connection");
            return server;
        }

        /// <summary>
        /// Checks whether the server certificate matches the certificate authority as announced through discovery
        /// </summary>
        /// <param name="server">The server </param>
        /// <param name="serverCertificate">The actual server certificate</param>
        /// <param name="certificateAuthority">The certificate authority as announced through discovery</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData CheckCertificateAuthority( ServerData server, X509Certificate2 serverCertificate, X509Certificate2 certificateAuthority )
        {
            return server;
        }

        /// <summary>
        /// Gets called in the case that the server did not announce its certificate through discovery
        /// </summary>
        /// <param name="server">The server that was connected</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData ServerUuidNotAnnounced( ServerData server )
        {
            _loggingChannel.Info( $"Server {server.Config.Name} did not announce certificates through discovery" );
            return server;
        }

        /// <summary>
        /// Gets called in the case that the server has a different UUID than what was announced during discovery
        /// </summary>
        /// <param name="server">The server that was connected</param>
        /// <param name="dnsGuid">The Server UUID found via DNS</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData ServerUuidDoesNotMatchDnsEntry( ServerData server, Guid dnsGuid )
        {
            _loggingChannel.Warn( $"Server UUID does not match expected value from Discovery. Expected {dnsGuid} but the actual server id is {server.Config.Uuid}" );
            return server;
        }

        /// <summary>
        /// Gets called in the case that the server has a UUID that is different than the UUID in the server certificate
        /// </summary>
        /// <param name="server">The server that was connected</param>
        /// <param name="serverIdFromCertificate">The Server UUID from the certificate</param>
        /// <param name="serverCertificate">The server certificate</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData ServerUuidDoesNotMatchCertificate( ServerData server, Guid serverIdFromCertificate, X509Certificate2 serverCertificate )
        {
            _loggingChannel.Warn( $"Server UUID does not match expected value from certificate. Expected {serverIdFromCertificate} but the actual server id is {server.Config.Uuid}" );
            return server;
        }

        /// <summary>
        /// Gets called in the case that the server has a certificate without a SAN matching to a server UUID
        /// </summary>
        /// <param name="server">The server that was connected</param>
        /// <param name="serverCertificate">The server certificate</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData ServerUuidNotContainedInCertificate( ServerData server, X509Certificate2 serverCertificate )
        {
            _loggingChannel.Warn( $"No server uuid could be found in the certificate for server {server.Config.Uuid}" );
            return server;
        }

        /// <summary>
        /// Gets called in the case that the server certificate could not be read
        /// </summary>
        /// <param name="server">The connected server</param>
        /// <returns>The connected server or null, if the connection should be refused</returns>
        protected virtual ServerData ServerCertificateCouldNotBeRead( ServerData server )
        {
            return null;
        }

        private string RestoreCa( IReadOnlyDictionary<string, string> details )
        {
            var sb = new StringBuilder();
            var line = 0;
            while(details.TryGetValue( "ca" + line, out var caLine ))
            {
                sb.Append( caLine );
                if(!caLine.EndsWith( "\n" ) && !caLine.EndsWith( "\r" ))
                {
                    sb.AppendLine();
                }
                line++;
            }
            return sb.ToString();
        }

        /// <inheritdoc />
        public virtual ServerData Connect( string host, int port )
        {
            return Connect( $"{host}:{port}", null, null, !host.StartsWith( "http" ) );
        }

        /// <summary>
        /// Load all server properties 
        /// </summary>
        protected ServerData GetServerData( GrpcChannel channel, Func<X509Certificate2> certificate )
        {
            var clientChannel = new SilaChannel(channel);
            var silaService = new SiLAServiceClient(clientChannel, _executionManager);
            var config = GetServerConfig(silaService);
            var info = GetServerInfo(silaService);
            var features = GetImplementedFeatures(silaService);
            var server = new ServerData(config, info, features, clientChannel, certificate?.Invoke());
            return server;
        }

        /// <summary>
        /// Load all server properties 
        /// </summary>
        public ServerData GetServerData( GrpcChannel channel )
        {
            return GetServerData( channel, null );
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
            return Connect( $"{host}:{port}", expectedServer, details, true );
        }

        /// <inheritdoc />
        public ServerData Connect( string host, int port, Guid? expectedServer, IReadOnlyDictionary<string, string> details )
        {
            return Connect( $"{host}:{port}", expectedServer, details, !host.StartsWith( "http" ) );
        }
    }
}

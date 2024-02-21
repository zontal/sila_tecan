using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Tecan.Sila2.Security
{
    /// <summary>
    /// Denotes a helper class used to generate certificates on the fly
    /// </summary>
    public static class CertificateGenerator
    {
        private static readonly ILog Log = LogManager.GetLogger( typeof( CertificateGenerator ) );

        /// <summary>
        /// Creates a new server certificate
        /// </summary>
        /// <param name="baseCertificate">The base CA certificate (must include private key) or null, if a self-signed certificate should be used</param>
        /// <param name="key">The key used for the certificate</param>
        /// <param name="serverGuid">The UUID of the server for which to generate the certificate</param>
        /// <returns>A X509 certificate that can be used for the server</returns>
        public static X509Certificate2 CreateServerCertificate( X509Certificate2 baseCertificate, RSA key, Guid? serverGuid )
        {
#if NET6_0_OR_GREATER
            var request = new CertificateRequest( "CN=SiLA2", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1 );

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension( false, false, 0, true ) );
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    true ) );

            if(serverGuid.HasValue)
            {
                // add server uuid
                request.CertificateExtensions.Add( new X509Extension( "1.3.6.1.4.1.58583", Encoding.ASCII.GetBytes( serverGuid.Value.ToString() ), false ) );
            }

            // Enhanced key usages
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection {
                    new Oid("1.3.6.1.5.5.7.3.2"), // TLS Client auth
                    new Oid("1.3.6.1.5.5.7.3.1")  // TLS Server auth
                    },
                    false ) );
            // add this subject key identifier
            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension( request.PublicKey, false ) );

            // cert serial is the epoch/unix timestamp
            var epoch = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );
            var unixTime = Convert.ToInt64( (DateTime.UtcNow - epoch).TotalSeconds );
            var serial = BitConverter.GetBytes( unixTime );

            if(baseCertificate != null)
            {
                return request
                    .Create( baseCertificate, DateTimeOffset.UtcNow.Subtract( TimeSpan.FromMinutes( 1 ) ), baseCertificate.NotAfter, serial )
                    .CopyWithPrivateKey( key );
            }
            else
            {
                return request.CreateSelfSigned( DateTimeOffset.UtcNow.Subtract( TimeSpan.FromMinutes( 1 ) ), DateTimeOffset.UtcNow.AddYears( 100 ) );
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// Generates a new root certificate
        /// </summary>
        /// <param name="key">The key for which to generate the root certificate</param>
        /// <returns>A certificate that can be used as CA</returns>
        public static X509Certificate2 CreateRootCertificate( RSA key )
        {
#if NET6_0_OR_GREATER
            var request = new CertificateRequest( "CN=Root", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1 );
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension( true, false, 0, true ) );
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign,
                    true ) );
            return request.CreateSelfSigned( DateTime.Now.Subtract( TimeSpan.FromMinutes( 1 ) ), DateTime.Now.AddYears( 100 ) );
#else
            return null;
#endif
        }



        /// <summary>
        /// Gets the server certificate to use
        /// </summary>
        /// <param name="context">The certificate context</param>
        /// <param name="serverUuid">The UUID of the server or null</param>
        /// <param name="certificateAuthority">The certificate authority used</param>
        /// <returns>A certificate that the server shall use for encrypted connections</returns>
        public static X509Certificate2 GetServerCertificate( CertificateContext context, Guid? serverUuid, out X509Certificate2 certificateAuthority )
        {
            certificateAuthority = null;
#if NET6_0_OR_GREATER
            if(context != null)
            {
                var key = RSA.Create();
                try
                {
                    var ca = context.CertificateAuthority != null ? LoadCertificateFromPem( context.CertificateAuthority ) : null;
                    if(ca != null && context.CertificateAuthorityKey != null)
                    {
                        var caKey = RSA.Create();
                        caKey.ImportRSAPrivateKey( ExtractFromPem( context.CertificateAuthorityKey ), out _ );
                        ca = ca.CopyWithPrivateKey( caKey );
                    }

                    if(context.Key != null)
                    {
                        key.ImportRSAPrivateKey( ExtractFromPem( context.Key ), out _ );
                    }

                    if(context.Certificate != null)
                    {
                        var certificate = LoadCertificateFromPem( context.Certificate );
                        if(certificate.NotBefore < DateTime.Now && certificate.NotAfter > DateTime.Now)
                        {
                            certificateAuthority = ca ?? certificate;
                            return CompensateWindowsEpheralKeysProblem( certificate.CopyWithPrivateKey( key ) );
                        }
                        else
                        {
                            Log.Warn( "The server certificate has expired." );
                        }
                    }

                    if(ca != null && ca.HasPrivateKey && ca.NotBefore < DateTime.Now && ca.NotAfter > DateTime.Now)
                    {
                        certificateAuthority = ca;
                        return CompensateWindowsEpheralKeysProblem( CreateServerCertificate( ca, key, serverUuid ) );
                    }
                    else
                    {
                        Log.Warn( "No certificate authority set or certificate authority expired" );
                    }
                }
                catch(Exception ex)
                {
                    Log.Error( "Error while trying to create certificate", ex );
                }
                return CompensateWindowsEpheralKeysProblem( CreateServerCertificate( null, key, serverUuid ) );
            }
#endif
            return null;
        }

        private static X509Certificate2 CompensateWindowsEpheralKeysProblem( X509Certificate2 cert )
        {
            if(cert == null || RuntimeInformation.IsOSPlatform( OSPlatform.Windows ))
            {
                // SSLStream on Windows throws with ephemeral key sets
                // workaround from https://github.com/dotnet/runtime/issues/23749#issuecomment-388231655
                var originalCert = cert;
                cert = new X509Certificate2( cert.Export( X509ContentType.Pkcs12 ) );
                originalCert.Dispose();
            }
            return cert;
        }

        /// <summary>
        /// Loads the certificate encoded in the provided PEM-encoding of the certificate into memory
        /// </summary>
        /// <param name="certificate">The PEM-encoded certificate</param>
        /// <returns>A X509Certificate2 representation of the certificate</returns>
        public static X509Certificate2 LoadCertificateFromPem( string certificate )
        {
            return new X509Certificate2( ExtractFromPem( certificate ) );
        }

        private static byte[] ExtractFromPem( string pem )
        {
#if NET6_0_OR_GREATER
            var data = PemEncoding.Find( pem );
            return Convert.FromBase64String( pem[data.Base64Data] );
#else
            var lines = pem.Split( '\n' );
            var startIndex = lines[0].StartsWith( "-" ) ? 1 : 0;
            var endIndex = lines[lines.Length - 1].StartsWith( "-" ) ? 1 : 0;
            var actualContent = string.Join( string.Empty, lines.Skip( startIndex ).Take( lines.Length - startIndex - endIndex ) );
            return Convert.FromBase64String( actualContent );
#endif
        }
    }
}

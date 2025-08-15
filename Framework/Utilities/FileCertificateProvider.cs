using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tecan.Sila2.Security;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a class that simply picks up already existing certificates, if any
    /// </summary>
    public class FileCertificateProvider : IServerCertificateProvider
    {
        private string _customCertificatePath;

        /// <inheritdoc />
        public CertificateContext CreateContext(Guid serverGuid) => CreateContext();

        /// <inheritdoc />
        public CertificateContext CreateContext() => CreateContext(_customCertificatePath);

        /// <summary>
        /// Creates a certificate context for the given custom server certificate location
        /// </summary>
        /// <param name="serverCertificatePath">The path to the server certificate</param>
        /// <returns>The certificate context</returns>
        public static CertificateContext CreateContext(string serverCertificatePath)
        {
            var cacert = ReadIfExists("ca.crt");
            var servercert = ReadIfExists(@"server.crt");
            var serverkey = ReadIfExists(serverCertificatePath ?? @"server.key");
            var caKey = ReadIfExists("ca.key");
            return new CertificateContext(servercert, serverkey, cacert, caKey);
        }

        private static string ReadIfExists( string path )
        {
            return File.Exists( path ) ? File.ReadAllText( path ) : null;
        }

        /// <inheritdoc />
        public void Initialize(string customCertificatePath)
        {
            _customCertificatePath = customCertificatePath;
        }
    }
}

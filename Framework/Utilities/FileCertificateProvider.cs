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
        /// <inheritdoc />
        public CertificateContext CreateContext(Guid serverGuid) => GetOrCreateDefaultContext();

        /// <inheritdoc />
        public CertificateContext CreateContext() => GetOrCreateDefaultContext();

        private static string ReadIfExists( string path )
        {
            return File.Exists( path ) ? File.ReadAllText( path ) : null;
        }

        private static CertificateContext _defaultContext;

        private static CertificateContext GetOrCreateDefaultContext()
        {
            if (_defaultContext == null)
            {
                var cacert = ReadIfExists("ca.crt");
                var servercert = ReadIfExists(@"server.crt");
                var serverkey = ReadIfExists(@"server.key");
                var caKey = ReadIfExists("ca.key");
                _defaultContext = new CertificateContext(servercert, serverkey, cacert, caKey);
            }
            return _defaultContext;
        }

        /// <summary>
        /// Gets the certificate context used by default
        /// </summary>
        public static CertificateContext DefaultContext => GetOrCreateDefaultContext();
    }
}

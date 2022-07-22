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
        public CertificateContext CreateContext( Guid serverGuid )
        {
            return CreateContext();
        }

        /// <inheritdoc />
        public CertificateContext CreateContext()
        {
            var cacert = ReadIfExists( "ca.crt" );
            var servercert = ReadIfExists( @"server.crt" );
            var serverkey = ReadIfExists( @"server.key" );
            var caKey = ReadIfExists( "ca.key" );
            return new CertificateContext( servercert, serverkey, cacert, caKey );
        }

        private string ReadIfExists( string path )
        {
            return File.Exists( path ) ? File.ReadAllText( path ) : null;
        }
    }
}

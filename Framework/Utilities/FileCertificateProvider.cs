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
            if(File.Exists( "server.crt" ) && File.Exists( "ca.crt" ) && File.Exists( "server.key" ))
            {
                var cacert = File.ReadAllText( @"ca.crt" );
                var servercert = File.ReadAllText( @"server.crt" );
                var serverkey = File.ReadAllText( @"server.key" );
                return new PemCertificateContext( servercert, serverkey, cacert );
            }
            else if(File.Exists( "server.pfx" ))
            {
                return new PfxCertificateContext( Path.GetFullPath( "server.pfx" ) );
            }
            else
            {
                return new PfxCertificateContext( null );
            }
        }
    }
}

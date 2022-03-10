using System;
using System.ComponentModel.Composition;
using Tecan.Sila2.Security;

namespace TestServer.NetCore
{
    [Export( typeof( IServerCertificateProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    public class NoCertificateProvider : IServerCertificateProvider
    {
        public CertificateContext CreateContext( Guid serverGuid )
        {
            return null;
        }

        public CertificateContext CreateContext()
        {
            return null;
        }
    }
}

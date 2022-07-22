using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Tecan.Sila2.Discovery
{
    internal class ServerCertificateValidator
    {
        private readonly X509Certificate2 _discoveryCertificate;
        private X509Certificate2 _actualCertificate;
        private X509Chain _chain;

        public ServerCertificateValidator( X509Certificate2 discoveryCertificate )
        {
            _discoveryCertificate = discoveryCertificate;
        }

        public bool ValidateActualCertificate( HttpRequestMessage request, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors )
        {
            _actualCertificate = cert ?? _actualCertificate;
            _chain = chain;
            return true;
        }

        public X509Certificate2 ServerCertificate => _actualCertificate;
    }
}

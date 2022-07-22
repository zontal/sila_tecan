using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace Tecan.Sila2.Security
{
    /// <summary>
    /// Denotes an abstract base class for a certificate context
    /// </summary>
    public class CertificateContext
    {
        /// <summary>
        /// Gets the PEM-encoded certificate authority used in this context
        /// </summary>
        [Obsolete( "Use CertificateAuthority instead" )]
        public string CA => CertificateAuthority;

        /// <summary>
        /// Gets the PEM-encoded certificate
        /// </summary>
        public string Certificate { get; }

        /// <summary>
        /// Gets the PEM-encoded private key for the certificate
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the PEM-encoded certificate authority used in this context
        /// </summary>
        public string CertificateAuthority { get; }

        /// <summary>
        /// Gets the PEM-encoded private key of the certificate authority used in this context
        /// </summary>
        public string CertificateAuthorityKey { get; }

        /// <summary>
        /// Creates a new certificate context
        /// </summary>
        /// <param name="certificate">the PEM-encoded certificate</param>
        /// <param name="key">the PEM-encoded private key for the certificate</param>
        /// <param name="certificateAuthority">the PEM-encoded certificate authority used in this context</param>
        public CertificateContext( string certificate, string key, string certificateAuthority ) : this(certificate, key, certificateAuthority, null)
        {
        }

        /// <summary>
        /// Creates a new certificate context
        /// </summary>
        /// <param name="certificate">the PEM-encoded certificate</param>
        /// <param name="key">the PEM-encoded private key for the certificate</param>
        /// <param name="certificateAuthority">the PEM-encoded certificate authority used in this context</param>
        /// <param name="certificateAuthorityKey">the PEM-encoded private key for the certificate authority or null</param>
        public CertificateContext( string certificate, string key, string certificateAuthority, string certificateAuthorityKey )
        {
            Certificate = certificate;
            Key = key;
            CertificateAuthority = certificateAuthority;
            CertificateAuthorityKey = certificateAuthorityKey;
        }
    }
}

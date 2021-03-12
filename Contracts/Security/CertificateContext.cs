using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Security
{
    /// <summary>
    /// Denotes an encryption context
    /// </summary>
    public class CertificateContext
    {
        /// <summary>
        /// Gets the PEM-encoded certificate authority used in this context
        /// </summary>
        public string CA { get; }

        /// <summary>
        /// Gets the PEM-encoded certificate
        /// </summary>
        public string Certificate { get; }

        /// <summary>
        /// Gets the PEM-encoded private key for the certificate
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Creates a new certificate context
        /// </summary>
        /// <param name="certificate">the PEM-encoded certificate</param>
        /// <param name="key">the PEM-encoded private key for the certificate</param>
        /// <param name="ca">the PEM-encoded certificate authority used in this context</param>
        public CertificateContext(string certificate, string key, string ca)
        {
            Certificate = certificate;
            Key = key;
            CA = ca;
        }
    }
}

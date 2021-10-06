using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Security
{
    /// <summary>
    /// Denotes an encryption context based on PFX certificate files
    /// </summary>
    public class PfxCertificateContext : CertificateContext
    {
        /// <summary>
        /// Gets the path to the pfx file
        /// </summary>
        public string PathToPfx
        {
            get;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="pathToPfx">The path to the pfx file that contains the certificate</param>
        public PfxCertificateContext(string pathToPfx)
        {
            PathToPfx = pathToPfx;
        }
    }
}

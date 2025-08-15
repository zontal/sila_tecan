using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Tecan.Sila2.Security
{
    /// <summary>
    /// Denotes a helper class with certificate helper functionality
    /// </summary>
    public static class CertificateUtilities
    {
        /// <summary>
        /// Tries to find the server UUID in the given certificate
        /// </summary>
        /// <param name="certificate">the certificate</param>
        /// <param name="serverUuid">the server UUID contained in the certificate or null, if no server UUID was present</param>
        /// <returns>true, if a server UUID was found, otherwise false</returns>
        public static bool TryFindServerUuid(X509Certificate2 certificate, out Guid serverUuid)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            foreach (var extension in certificate.Extensions)
            {
                if (extension.Oid.Value.ToString() == "1.3.6.1.4.1.58583")
                {
                    var value = Encoding.ASCII.GetString(extension.RawData);
                    if (value.Length >= 36 && Guid.TryParse(value.Substring(value.Length - 36), out serverUuid))
                    {
                        return true;
                    }

                }
            }

            serverUuid = Guid.Empty;
            return false;
        }
    }
}

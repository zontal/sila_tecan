using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Security
{
    /// <summary>
    /// Denotes a component that can create a certificate context for a server or client
    /// </summary>
    public interface IServerCertificateProvider
    {
        /// <summary>
        /// Gets or creates the security context for the server with the provided guid
        /// </summary>
        /// <param name="serverGuid">The unique id of the server</param>
        /// <returns>A certificate context or null, if no security context could be created</returns>
        CertificateContext CreateContext( Guid serverGuid );


        /// <summary>
        /// Gets or creates the security context for the server without the context of a server uuid
        /// </summary>
        /// <returns>A certificate context or null, if no security context could be created</returns>
        CertificateContext CreateContext();
    }
}

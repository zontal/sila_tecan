using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Authorization
{
    /// <summary>
    /// Denotes the interface for an authorization service
    /// </summary>
    public partial interface IAuthorizationService
    {
        /// <summary>
        /// Gets the interceptor for the access token delivered by the client for authenticated requests
        /// </summary>
        [MetadataType(typeof(string))]
        IRequestInterceptor AccessToken { get; }
    }
}

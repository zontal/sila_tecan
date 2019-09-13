using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Authorization
{
    public partial interface IAuthorizationService
    {
        [MetadataType(typeof(string))]
        IRequestInterceptor AccessToken { get; }
    }
}

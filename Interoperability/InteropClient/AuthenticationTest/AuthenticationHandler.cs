using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Authentication;

namespace Tecan.Sila2.Interop.Client.AuthenticationTest
{

    internal class AuthenticationHandler : LocalAuthenticationHandler
    {
        public override Credentials GetCredentials(ServerData server)
        {
            return new Credentials("test", "test");
        }
    }
}

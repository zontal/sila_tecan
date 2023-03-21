using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;

namespace Tecan.Sila2.Interop.Server.AuthenticationTest
{
    [Export( typeof( IAuthenticationTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class AuthenticationTest : IAuthenticationTest
    {
        public void RequiresToken()
        {
        }

        public void RequiresTokenForBinaryUpload(Stream binaryToUpload)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.IntegrationTests
{
    [Export( typeof( IAuthConfigurationProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class AuthorizationProvider : StaticAuthConfigurationProvider
    {
        [ImportingConstructor]
        public AuthorizationProvider( IConfigurationStore configurationStore ) : base( null, null, configurationStore )
        {
        }
    }
}

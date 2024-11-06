using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Tecan.Sila2.AuthorizationProvider;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.Interop.Server.AuthenticationTest
{
    [Export( typeof( IAuthConfigurationProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class AuthenticationProvider : StaticAuthConfigurationProvider
    {
        [ImportingConstructor]
        public AuthenticationProvider( IConfigurationStore configurationStore ) : this(configurationStore, new ServerConnector(new DiscoveryExecutionManager()))
        {
        }

        private AuthenticationProvider(IConfigurationStore configurationStore, IServerConnector connector )
            : base(connector, new ServerDiscovery(connector), configurationStore) { }

        protected override AuthConfiguration CreateDefaultConfig()
        {
            return new AuthConfiguration
            {
                LocalUser = new[]
                {
                    new User
                    {
                        Name = "test",
                        Password = HashPassword("test"),
                        AllowWildcard = true,
                    }
                }
            };
        }
    }
}

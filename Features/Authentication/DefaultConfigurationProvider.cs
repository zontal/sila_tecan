using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace Tecan.Sila2
{
    [Export]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class DefaultConfigurationProvider : AuthConfigurationProviderBase
    {
        private const string ConfigName = "Auth";
        private readonly IConfigurationStore _configurationStore;

        [ImportingConstructor]
        public DefaultConfigurationProvider( IServerConnector serverConnector, IServerDiscovery discovery, IConfigurationStore configurationStore )
            : base( discovery, serverConnector )
        {
            _configurationStore = configurationStore;
        }

        protected override User Login( string username, string password, AuthConfiguration configuration )
        {
            return configuration.LocalUser?.FirstOrDefault( u => string.Equals( u.Name, username, System.StringComparison.OrdinalIgnoreCase ) &&
                PasswordHasher.VerifyPassword( u.Password, password ) );
        }

        protected override AuthConfiguration LoadConfiguration()
        {
            if(File.Exists( ConfigName ))
            {
                return _configurationStore.Read<AuthConfiguration>( ConfigName );
            }

            return new AuthConfiguration()
            {
                LocalUser = new[]
                {
                    new User()
                    {
                        Name = "admin",
                        Password = PasswordHasher.HashPassword("admin"),
                        AllowWildcard = true
                    }
                }
            };
        }

        protected override void SaveConfiguration( AuthConfiguration configuration )
        {
            _configurationStore.Write( ConfigName, configuration );
        }
    }
}

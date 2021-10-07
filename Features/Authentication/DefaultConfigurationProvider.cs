using System.ComponentModel.Composition;
using System.IO;

namespace Tecan.Sila2
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class DefaultConfigurationProvider : AuthConfigurationProviderBase
    {
        private const string ConfigName = "Auth";
        private readonly IConfigurationStore _configurationStore;

        [ImportingConstructor]
        public DefaultConfigurationProvider(IServerConnector serverConnector, IServerDiscovery discovery, IConfigurationStore configurationStore)
            : base( discovery, serverConnector)
        {
            _configurationStore = configurationStore;
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
                        Password = "admin",
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

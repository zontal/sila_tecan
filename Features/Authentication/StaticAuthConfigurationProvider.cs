using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes an authentication/authorization provider that reads users from the configuration store
    /// </summary>
    public class StaticAuthConfigurationProvider : AuthConfigurationProviderBase
    {
        private const string ConfigName = "Auth";
        private readonly IConfigurationStore _configurationStore;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="serverConnector">The server connector</param>
        /// <param name="discovery">The discovery</param>
        /// <param name="configurationStore">The configuration store</param>
        public StaticAuthConfigurationProvider( IServerConnector serverConnector, IServerDiscovery discovery, IConfigurationStore configurationStore )
            : base( discovery, serverConnector )
        {
            _configurationStore = configurationStore;
        }

        /// <inheritdoc />
        protected override User Login( string username, string password, AuthConfiguration configuration )
        {
            return configuration.LocalUser?.FirstOrDefault( u => string.Equals( u.Name, username, System.StringComparison.OrdinalIgnoreCase ) &&
                PasswordHasher.VerifyPassword( u.Password, password ) );
        }

        /// <inheritdoc />
        protected override AuthConfiguration LoadConfiguration()
        {
            if (_configurationStore.Exists(ConfigName, typeof(AuthConfiguration)))
            {
                return _configurationStore.Read<AuthConfiguration>( ConfigName );
            }

            return CreateDefaultConfig();
        }

        /// <summary>
        /// Creates the default configuration in case no configuration can be found in the config store
        /// </summary>
        /// <returns>An Auth configuration instance</returns>
        protected virtual AuthConfiguration CreateDefaultConfig()
        {
            return new AuthConfiguration()
            {
                LocalUser = new[]
                {
                    new User()
                    {
                        Name = "admin",
                        Password = HashPassword("admin"),
                        AllowWildcard = true
                    }
                }
            };
        }

        /// <summary>
        /// Calculates a hash of the given password
        /// </summary>
        /// <param name="password">The password</param>
        /// <returns>A hash of the password that can be saved</returns>
        protected string HashPassword( string password )
        {
            return PasswordHasher.HashPassword( password );
        }

        /// <inheritdoc />
        protected override void SaveConfiguration( AuthConfiguration configuration )
        {
            _configurationStore.Write( ConfigName, configuration );
        }
    }
}

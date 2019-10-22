using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2
{
    internal class DefaultConfigurationProvider : AuthConfigurationProviderBase
    {
        private const string ConfigName = "Auth.config";
        private readonly XmlSerializer _serializer = new XmlSerializer( typeof( AuthConfiguration ) );

        public DefaultConfigurationProvider(ILoggingChannelFactory loggingChannelFactory)
            : base(loggingChannelFactory, new ServerDiscovery(loggingChannelFactory, new DiscoveryExecutionManager()))
        {
        }

        protected override AuthConfiguration LoadConfiguration()
        {
            if(File.Exists( ConfigName ))
            {
                using(var stream = File.OpenRead( ConfigName ))
                {
                    return _serializer.Deserialize( stream ) as AuthConfiguration;
                }
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
            using( var stream = File.OpenWrite(ConfigName) )
            {
                _serializer.Serialize(stream, configuration);
            }
        }
    }
}

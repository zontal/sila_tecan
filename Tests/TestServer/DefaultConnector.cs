using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.IntegrationTests
{
    [Export(typeof(IServerConnector))]
    internal class DefaultConnector : ServerConnector
    {
        [ImportingConstructor]
        public DefaultConnector( ILoggingChannelFactory loggingChannelFactory ) : base( new DiscoveryExecutionManager(), loggingChannelFactory )
        {
        }
    }
}

using System.ComponentModel.Composition;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.IntegrationTests
{
    [Export(typeof(IServerConnector))]
    internal class DefaultConnector : ServerConnector
    {
        [ImportingConstructor]
        public DefaultConnector() : base( new DiscoveryExecutionManager() )
        {
        }
    }
}

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.Client.ExecutionManagement
{
    [Export( typeof( IExecutionManagerFactory ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ExecutionManagerFactory : IExecutionManagerFactory
    {
        private readonly IEnumerable<IRequestInterceptor> _interceptors;
        private readonly Dictionary<ServerData, IClientExecutionManager> _cachedExecutionManagers = new Dictionary<ServerData, IClientExecutionManager>();

        [ImportingConstructor]
        public ExecutionManagerFactory( [ImportMany] IEnumerable<IRequestInterceptor> interceptors )
        {
            _interceptors = interceptors;
        }

        public IClientExecutionManager CreateExecutionManager( ServerData server )
        {
            if(server == null)
            {
                return new DiscoveryExecutionManager();
            }

            if(!_cachedExecutionManagers.TryGetValue( server, out var executionManager ))
            {
                executionManager = new ExecutionManager( server, _interceptors );
                _cachedExecutionManagers.Add( server, executionManager );
            }

            return executionManager;
        }
    }
}

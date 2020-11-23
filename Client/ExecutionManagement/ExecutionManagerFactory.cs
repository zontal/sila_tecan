using System.Collections.Generic;
using System.ComponentModel.Composition;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.Client.ExecutionManagement
{
    /// <summary>
    /// Denotes a default implementation of an execution manager factory
    /// </summary>
    [Export( typeof( IExecutionManagerFactory ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    public class ExecutionManagerFactory : IExecutionManagerFactory
    {
        private readonly IEnumerable<IClientRequestInterceptor> _interceptors;
        private readonly Dictionary<ServerData, IClientExecutionManager> _cachedExecutionManagers = new Dictionary<ServerData, IClientExecutionManager>();

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="interceptors">The interceptors used for this factory</param>
        [ImportingConstructor]
        public ExecutionManagerFactory( [ImportMany] IEnumerable<IClientRequestInterceptor> interceptors )
        {
            _interceptors = interceptors;
        }

        /// <inheritdoc />
        public IClientExecutionManager CreateExecutionManager( ServerData server )
        {
            var grpcServer = server as GrpcServerData;
            if(grpcServer == null)
            {
                return new DiscoveryExecutionManager();
            }

            if(!_cachedExecutionManagers.TryGetValue( server, out var executionManager ))
            {
                executionManager = new ExecutionManager( grpcServer, _interceptors );
                _cachedExecutionManagers.Add( server, executionManager );
            }

            return executionManager;
        }
    }
}

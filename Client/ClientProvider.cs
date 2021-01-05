using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// A simple client provider implementation
    /// </summary>
    [Export( typeof( IClientProvider ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    public class ClientProvider : IClientProvider
    {
        private readonly Dictionary<Type, IClientFactory> _factories;
        private readonly IExecutionManagerFactory _executionManagerFactory;

        /// <summary>
        /// Initializes a new client provider
        /// </summary>
        /// <param name="executionManagerFactory">The factory for the execution managers</param>
        /// <param name="factories">The factories for the client classes</param>
        public ClientProvider( IExecutionManagerFactory executionManagerFactory, [ImportMany] IEnumerable<IClientFactory> factories)
        {
            _executionManagerFactory = executionManagerFactory;
            _factories = factories?.ToDictionary( fac => fac.InterfaceType );
        }

        /// <inheritdoc />
        public bool TryCreateClient<TClient>( ServerData server, out TClient client ) where TClient : class
        {
            if (_factories.TryGetValue(typeof(TClient), out var factory))
            {
                if (server.Features.Any(f => f.FullyQualifiedIdentifier == factory.FeatureIdentifier))
                {
                    var executionManager = _executionManagerFactory.CreateExecutionManager( server );
                    client = factory.CreateClient( server.Channel, executionManager ) as TClient;
                    return true;
                }
                else
                {
                    client = null;
                    return false;
                }
            }
            else
            {
                throw new NotSupportedException( $"No registered factory is able to instantiate a client of type {typeof( TClient ).Name}." );
            }
        }
    }
}

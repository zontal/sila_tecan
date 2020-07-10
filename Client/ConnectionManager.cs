using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.Client
{
    [Export( typeof( IConnectionManager ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ConnectionManager : IConnectionManager
    {
        private Dictionary<Guid, ServerData> _knownServers = new Dictionary<Guid, ServerData>();
        private readonly IServerDiscovery _discovery;

        [ImportingConstructor]
        public ConnectionManager( ILoggingChannelFactory loggingChannelFactory, IServerConnector serverConnector )
        {
            _discovery = new ServerDiscovery( loggingChannelFactory, serverConnector );
        }

        public IEnumerable<ServerData> KnownServers => _knownServers.Values;


        public void RefreshServerList( TimeSpan timeout )
        {
            var newServers = new Dictionary<Guid, ServerData>();
            foreach( var server in _discovery.GetServers(timeout) )
            {
                newServers.Add( server.Config.Uuid, _knownServers.TryGetValue( server.Config.Uuid, out var previousServer ) ? previousServer : server );
            }

            _knownServers = newServers;
        }

        public ServerData Connect( Guid serverUuid, TimeSpan timeout = default )
        {
            if( !_knownServers.TryGetValue( serverUuid, out var server ) )
            {
                server = _discovery.Connect( serverUuid, timeout );
                _knownServers.Add(serverUuid, server);
            }

            return server;
        }
    }
}

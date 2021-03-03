using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Tecan.MAP.Contracts.Services.Logging;

namespace Tecan.Sila2.Server.ConnectionConfiguration
{
    [Export(typeof(IConnectionConfigurationService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ConnectionConfigurationService : IConnectionConfigurationService
    {
        private readonly ISiLAServer _server;
        private readonly Dictionary<string, ClientEntry> _connections = new Dictionary<string, ClientEntry>();
        private readonly ILoggingChannel _loggingChannel;
        private readonly IConfigurationStore _configurationStore;
        private bool _isEnabled;

        [ImportingConstructor]
        public ConnectionConfigurationService(ISiLAServer server, ILoggingChannelFactory loggingChannelFactory, IConfigurationStore configurationStore)
        {
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof( ConnectionConfigurationService ) );
            _configurationStore = configurationStore;
            _server = server;
        }

        public bool ServerInitiatedConnectionModeStatus => _isEnabled;

        public ICollection<ConfiguredSiLAClient> ConfiguredSiLAClients => _connections.Select( client => new ConfiguredSiLAClient( client.Key, client.Value.Host, client.Value.Port ) ).ToList();

        public void ConnectSiLAClient( [MaximalLength( 255 )] string clientName, [MaximalLength( 255 ), SilaDisplayName( "SiLA Client Host" )] string siLAClientHost, [MaximalInclusive( 65536 ), MinimalExclusive( 0 ), SilaDisplayName( "SiLA Client Port" )] long siLAClientPort, [SilaDisplayName( "Persist connection" )] bool persist )
        {
            if (!_isEnabled)
            {
                throw new Exception( "Connections are disabled." );
            }
            lock(_connections)
            {
                if (_connections.ContainsKey(clientName))
                {
                    throw new Exception( $"A client with the name {clientName} already exists." );
                }
                _loggingChannel.Log( $"Connecting to {clientName}...".ToString, Severity.Info );
                var channel = _server.ConnectToPool( $"{siLAClientHost}:{siLAClientPort}" );
                _loggingChannel.Log( $"Connection to {clientName} ({siLAClientHost}:{siLAClientPort}) established successfully".ToString, Severity.Info );
                _connections.Add( clientName, new ClientEntry( siLAClientHost, (int)siLAClientPort, channel ) );
            }
            if (persist)
            {
                _configurationStore.Write( GetName( clientName ), new ClientConnection
                {
                    Name = clientName,
                    Host = siLAClientHost,
                    Port = (int)siLAClientPort
                } );
            }
        }

        private string GetName( string clientName )
        {
            return new string( clientName.Where( char.IsLetterOrDigit ).ToArray() );
        }

        public void DisableServerInitiatedConnectionMode()
        {
            lock(_connections)
            {
                _loggingChannel.Log( "Disconnecting all clients".ToString, Severity.Info );
                foreach(var connection in _connections.Values)
                {
                    connection.Channel.Dispose();
                }
                _connections.Clear();
                _isEnabled = false;
            }
        }

        public void DisconnectSiLAClient( [MaximalLength( 255 )] string clientName, [SilaDisplayName( "Remove connection" )] bool remove )
        {
            if(_connections.TryGetValue( clientName, out var channel ))
            {
                lock(_connections)
                {
                    _loggingChannel.Log( $"Disconnecting client {clientName}".ToString, Severity.Info );
                    channel.Channel.Dispose();
                    _connections.Remove( clientName );
                }
            }
            else
            {
                _loggingChannel.Log( $"No client '{clientName}' connected.".ToString, Severity.Warning );
            }
            if (remove)
            {
                _configurationStore.Delete( GetName( clientName ), typeof( ClientConnection ) );
            }
        }

        public void EnableServerInitiatedConnectionMode()
        {
            _isEnabled = true;
            foreach(var configName in _configurationStore.GetNames(typeof(ClientConnection)))
            {
                var configuration = _configurationStore.Read<ClientConnection>( configName );
                ConnectSiLAClient( configuration.Name, configuration.Host, configuration.Port, false );
            }
        }

        private class ClientEntry
        {
            public string Host { get; }

            public int Port { get; }

            public IDisposable Channel { get; }

            public ClientEntry(string host, int port, IDisposable channel)
            {
                Host = host;
                Port = port;
                Channel = channel;
            }
        }
    }
}

using Common.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Tecan.Sila2.Server.ConnectionConfiguration
{
    [Export( typeof( IConnectionConfigurationService ) )]
    [Export(typeof(IConnectionConfigurationStatus))]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ConnectionConfigurationService : IConnectionConfigurationService, IConnectionConfigurationStatus
    {
        private readonly ISiLAServer _server;
        private readonly Dictionary<string, ClientEntry> _connections = new Dictionary<string, ClientEntry>();
        private readonly ILog _loggingChannel = LogManager.GetLogger<ConnectionConfigurationService>();
        private readonly IConfigurationStore _configurationStore;
        private readonly ServerStartInformation _serverStartInformation;
        private readonly ClientConnectionConfig _config;
        private bool _isEnabled;

        [ImportingConstructor]
        public ConnectionConfigurationService(ISiLAServer server, IConfigurationStore configurationStore, ServerStartInformation serverStartInformation)
        {
            _configurationStore = configurationStore;
            _server = server;
            _serverStartInformation = serverStartInformation;

            try
            {
                _config = _configurationStore.Read<ClientConnectionConfig>(serverStartInformation.ConfigName);
                if (_config != null)
                {
                    _config.Connection ??= Array.Empty<ClientConnection>();
                }
            }
            catch (Exception ex)
            {
                _loggingChannel.Warn("Error loading connection configuration.", ex);
                _config = new ClientConnectionConfig
                {
                    Connection = Array.Empty<ClientConnection>()
                };
            }
        }

        public bool ServerInitiatedConnectionModeStatus => _isEnabled;

        public ICollection<ConfiguredSiLAClient> ConfiguredSiLAClients => _connections.Select( client => new ConfiguredSiLAClient( client.Key, client.Value.Host, client.Value.Port ) ).ToList();

        public void ConnectSiLAClient( [MaximalLength( 255 )] string clientName, [MaximalLength( 255 ), SilaDisplayName( "SiLA Client Host" )] string siLAClientHost, [MaximalInclusive( 65536 ), MinimalExclusive( 0 ), SilaDisplayName( "SiLA Client Port" )] long siLAClientPort, [SilaDisplayName( "Persist connection" )] bool persist )
        {
            if(!_isEnabled)
            {
                throw new Exception( "Connections are disabled." );
            }
            lock(_connections)
            {
                if (_connections.ContainsKey(clientName))
                {
                    throw new Exception($"A client with the name {clientName} already exists.");
                }
                _loggingChannel.Info($"Connecting to {clientName}...");
                ConnectCore(clientName, siLAClientHost, siLAClientPort);
            }
            if (persist)
            {
                var newConnections = new ClientConnection[_connections.Count + 1];
                Array.Copy(_config.Connection, newConnections, _config.Connection.Length);
                newConnections[newConnections.Length - 1] = new ClientConnection
                {
                    Name = clientName,
                    Host = siLAClientHost,
                    Port = (int)siLAClientPort
                };
                TrySaveConfiguration();
            }
        }

        private void ConnectCore(string clientName, string siLAClientHost, long siLAClientPort)
        {
            var channel = _server.ConnectToPool($"{siLAClientHost}:{siLAClientPort}");
            _loggingChannel.Info($"Connection to {clientName} ({siLAClientHost}:{siLAClientPort}) established successfully");
            _connections[clientName] = new ClientEntry(siLAClientHost, (int)siLAClientPort, channel);
        }

        private void TrySaveConfiguration()
        {
            try
            {
                _configurationStore.Write(_serverStartInformation.ConfigName, _config);
            }
            catch (Exception ex)
            {
                _loggingChannel.Warn( "Could not persist client connection configuration", ex );
            }
        }

        public void DisableServerInitiatedConnectionMode()
        {
            if(_isEnabled)
            {
                lock(_connections)
                {
                    _loggingChannel.Info( "Disconnecting all clients" );
                    foreach(var connection in _connections.Values)
                    {
                        connection.Channel.Dispose();
                    }
                    _connections.Clear();
                    _isEnabled = false;
                }
                TrySetConnectionmodeEnabled( false );
            }
        }

        public void DisconnectSiLAClient( [MaximalLength( 255 )] string clientName, [SilaDisplayName( "Remove connection" )] bool remove )
        {
            if(_connections.TryGetValue( clientName, out var channel ))
            {
                lock(_connections)
                {
                    _loggingChannel.Info( $"Disconnecting client {clientName}" );
                    channel.Channel.Dispose();
                    if (!_connections.Remove( clientName ))
                    {
                        _loggingChannel.Warn($"Client named {clientName} could not be removed.");
                        return;
                    }
                }
                if (remove)
                {
                    _config.Connection = _config.Connection.Where( c => c.Name != clientName ).ToArray();
                    TrySaveConfiguration();
                }
            }
            else
            {
                _loggingChannel.Warn( $"No client '{clientName}' connected." );
            }
        }

        public void EnableServerInitiatedConnectionMode()
        {
            if(!_isEnabled)
            {
                _isEnabled = true;
                foreach(var configName in _configurationStore.GetNames( typeof( ClientConnection ) ))
                {
                    var configuration = _configurationStore.Read<ClientConnection>( configName );
                    ConnectSiLAClient( configuration.Name, configuration.Host, configuration.Port, false );
                }

                TrySetConnectionmodeEnabled( true );
            }
        }

        private void TrySetConnectionmodeEnabled( bool enabled )
        {
            try
            {
                var currentConfiguration = _configurationStore.Read<ServerConfiguration>( _serverStartInformation.ConfigName );
                if(currentConfiguration.EnableServerConnections != enabled)
                {
                    currentConfiguration.EnableServerConnections = enabled;
                    _configurationStore.Write(_serverStartInformation.ConfigName, currentConfiguration);
                }
            }
            catch(Exception ex)
            {
                _loggingChannel.Error( "Could not persist connection mode", ex );
            }
        }

        public void Start()
        { 
            lock (_connections)
            {
                foreach (var configuredConnection in _config.Connection)
                {
                    ConnectCore(configuredConnection.Name, configuredConnection.Host, configuredConnection.Port);
                }
            }
        }

        private class ClientEntry
        {
            public string Host { get; }

            public int Port { get; }

            public IDisposable Channel { get; }

            public ClientEntry( string host, int port, IDisposable channel )
            {
                Host = host;
                Port = port;
                Channel = channel;
            }
        }
    }
}

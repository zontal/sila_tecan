using Common.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Tecan.Sila2.Client;
using Tecan.Sila2.Locking.LockController;

namespace Tecan.Sila2.Locking
{
    /// <summary>
    /// Denotes an interceptor that handles locking of a server
    /// </summary>
    [Export( typeof( IClientRequestInterceptor ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    public class LockingInterceptor : IClientRequestInterceptor, IDisposable
    {
        private readonly ConcurrentDictionary<ServerData, ServerLockInformation> _serverLockData = new ConcurrentDictionary<ServerData, ServerLockInformation>();

        /// <inheritdoc />
        public string MetadataIdentifier => "org.silastandard/core/LockController/v1/Metadata/LockIdentifier";

        private readonly TimeSpan _expiryThreshold;
        private readonly TimeSpan _lockTime;
        private readonly TimeSpan _lockRenewTime;
        private readonly int _timerTick;

        private readonly Timer _lockRenewTimer;
        private bool _isDisposed;
        private int _timerDuration;

        private readonly ILog _loggingChannel = LogManager.GetLogger( typeof( LockingInterceptor ) );

        /// <summary>
        /// Creates a new instance
        /// </summary>
        [ImportingConstructor]
        public LockingInterceptor([ImportMany] IEnumerable<IConfigurationStore> configStores)
        {
            var config = configStores
                ?.Select(store => store.Read<LockConfiguration>("LockConfig"))
                .FirstOrDefault(c => c != null);

            if (config != null )
            {
                _expiryThreshold = TimeSpan.FromSeconds( config.ExpiryThreshold );
                _lockTime = TimeSpan.FromSeconds( config.LockTime );
                _lockRenewTime = TimeSpan.FromSeconds( config.RefreshTime );
                _timerTick = (int)(config.ExpiryCheckInterval * 1000);
            }
            else
            {
                _expiryThreshold = TimeSpan.FromSeconds(2);
                _lockRenewTime = _lockTime = TimeSpan.FromSeconds(30);
                _timerTick = 5_000;
            }

            _lockRenewTimer = new Timer(CheckExpiry);
        }

        /// <summary>
        /// Extends locks where required
        /// </summary>
        public void ExtendLocks()
        {
            foreach (var lockData in _serverLockData.Values)
            {
                lockData.ExtendIfNecessary(_expiryThreshold, _lockRenewTime);
            }
        }

        private void CheckExpiry(object state)
        {
            try
            {
                ExtendLocks();
            }
            catch (Exception ex)
            {
                _loggingChannel.Error("Error checking expiry", ex);
            }
        }



        /// <inheritdoc />
        public IClientRequestInterception Intercept( ServerData server, string interceptedCommand, IClientExecutionManager executionManager, IDictionary<string, byte[]> metadata )
        {
            if (Interlocked.CompareExchange(ref _timerDuration, _timerTick, 0) == 0)
            {
                _loggingChannel.Info("Start expiry timer");
                _lockRenewTimer.Change(_timerTick, _timerTick);
            }
            if(!_serverLockData.TryGetValue( server, out var lockData ))
            {
                var lockingClient = new LockControllerClient( server.Channel, executionManager );
                var lockingIdentifier = Guid.NewGuid().ToString();
                lockData = new ServerLockInformation( lockingClient, lockingIdentifier, this, server );
                _serverLockData.AddOrUpdate( server, lockData, (_,ex) => ex );
            }
            lockData.RegisterCall(_lockTime);
            metadata.Add( MetadataIdentifier, ByteSerializer.ToByteArray( new PropertyResponse<StringDto>( lockData.LockIdentifier ) ) );
            return lockData;
        }

        private class ServerLockInformation : IClientRequestInterception
        {
            private readonly ILockController _lockController;
            private readonly string _lockIdentifier;
            private int _interestedParties;
            private DateTime _lockExpiry;

            private readonly LockingInterceptor _parent;
            private readonly ServerData _server;

            public void RegisterCall(TimeSpan lockTime)
            {
                if(Interlocked.Increment( ref _interestedParties ) == 1 || DateTime.Now > _lockExpiry)
                {
                    _parent._loggingChannel.Info($"Locking server {_server.Config.Uuid} with lock identifier {_lockIdentifier}.");
                    _lockExpiry = DateTime.Now + lockTime;
                    _lockController.LockServer( _lockIdentifier, (long)lockTime.TotalSeconds );
                }
            }

            public void ExtendIfNecessary(TimeSpan threshold, TimeSpan lockTime)
            {
                if (_interestedParties > 0 && (_lockExpiry - DateTime.Now) < threshold)
                {
                    _parent._loggingChannel.Info($"Extending lock for server {_server.Config.Uuid} with lock identifier {_lockIdentifier}.");
                    _lockController.LockServer(_lockIdentifier, (long)lockTime.TotalSeconds);
                }
            }

            private void RegisterFinished()
            {
                if (Interlocked.Decrement(ref _interestedParties) == 0)
                {
                    if (_lockExpiry < DateTime.Now)
                    {
                        _parent._loggingChannel.Info($"Unlocking server {_server.Config.Uuid}.");
                        _lockController.UnlockServer(_lockIdentifier);
                    }
                    _parent._serverLockData.TryRemove(_server, out _);
                }
            }

            public string LockIdentifier => _lockIdentifier;

            public ServerLockInformation( ILockController lockController, string lockIdentifier, LockingInterceptor parent, ServerData serverData )
            {
                _lockController = lockController;
                _lockIdentifier = lockIdentifier;
                _parent = parent;
                _server = serverData;
            }

            public void CompleteSuccessfully()
            {
                RegisterFinished();
            }

            public void CompleteWithError( Exception exception )
            {
                RegisterFinished();
            }
        }

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _lockRenewTimer.Dispose();
                }

                _isDisposed = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
    public class LockingInterceptor : IClientRequestInterceptor
    {
        private readonly ConditionalWeakTable<ServerData, ServerLockInformation> _serverLockData = new ConditionalWeakTable<ServerData, ServerLockInformation>();

        /// <inheritdoc />
        public string MetadataIdentifier => "org.silastandard/core/LockController/v1/Metadata/LockIdentifier";

        /// <inheritdoc />
        public IClientRequestInterception Intercept( ServerData server, IClientExecutionManager executionManager, IDictionary<string, byte[]> metadata )
        {
            if(!_serverLockData.TryGetValue( server, out var lockData ))
            {
                var lockingClient = new LockControllerClient( server.Channel, executionManager );
                var lockingIdentifier = Guid.NewGuid().ToString();
                lockData = new ServerLockInformation( lockingClient, lockingIdentifier );
                _serverLockData.Add( server, lockData );
            }
            lockData.RegisterCall();
            metadata.Add( MetadataIdentifier, ByteSerializer.ToByteArray( new PropertyResponse<StringDto>( lockData.LockIdentifier ) ) );
            return lockData;
        }

        private class ServerLockInformation : IClientRequestInterception
        {
            private readonly ILockController _lockController;
            private readonly string _lockIdentifier;
            private int _interestedParties;
            private static readonly TimeSpan RefreshLockInterval = TimeSpan.FromSeconds( 20 );
            private DateTime _lockExpiry;

            public void RegisterCall()
            {
                if(Interlocked.Increment( ref _interestedParties ) == 1 || DateTime.Now > _lockExpiry)
                {
                    _lockExpiry = DateTime.Now + RefreshLockInterval;
                    _lockController.LockServer( _lockIdentifier, 30 );
                }
            }

            private void RegisterFinished()
            {
                if(Interlocked.Decrement( ref _interestedParties ) == 0)
                {
                    _lockController.UnlockServer( _lockIdentifier );
                }
            }

            public string LockIdentifier => _lockIdentifier;

            public ServerLockInformation( ILockController lockController, string lockIdentifier )
            {
                _lockController = lockController;
                _lockIdentifier = lockIdentifier;
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
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Locking
{
    [Export(typeof(ILockController))]
    [Export(typeof(IRequestInterceptor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class LockControllerService : LockIdentifierInterceptorBase, ILockController, IRequestInterceptor
    {
        private DateTime _expiry;
        private string _lock;
        private readonly HashSet<string> _commandsToLock;

        [ImportingConstructor]
        public LockControllerService(IConfigurationStore configurationStore)
        {
            if (configurationStore.Exists(nameof(LockControllerService), typeof(LockingConfiguration)))
            {
                var config = configurationStore.Read<LockingConfiguration>( nameof( LockControllerService ) );
                _commandsToLock = new HashSet<string>( config.Command );
            }
        }

        public LockControllerService()
        {
        }

        public bool IsLocked => DateTime.Now < _expiry;

        public IRequestInterceptor LockIdentifier => this;

        public override int Priority => 2;

        public override bool AppliesToCommands => true;

        public override bool AppliesToProperties => false;

        public override bool IsInterceptRequired(Feature feature, string commandIdentifier)
        {
            if (_commandsToLock != null)
            {
                return _commandsToLock.Contains( commandIdentifier );
            }
            return !commandIdentifier.StartsWith("org.silastandard/core");
        }

        public void LockServer(string lockIdentifier, long timeout)
        {
            lock (this)
            {
                var newExpiry = DateTime.Now + TimeSpan.FromSeconds(timeout);
                if (IsLockObtainedBy(lockIdentifier) || !IsLocked)
                {
                    _expiry = newExpiry;
                    _lock = lockIdentifier;
                }
                else
                {
                    throw CreateAlreadyLockedException();
                }
            }
        }

        private Exception CreateAlreadyLockedException()
        {
            return new ServerAlreadyLockedException($"The server is already locked with lock identifier {_lock} until {_expiry}.");
        }

        public void UnlockServer(string lockIdentifier)
        {
            lock (this)
            {
                if (IsLocked)
                {
                    if (IsLockObtainedBy(lockIdentifier))
                    {
                        _lock = null;
                        _expiry = DateTime.MinValue;
                    }
                    else
                    {
                        throw CreateAlreadyLockedException();
                    }
                }
                else
                {
                    throw new ServerNotLockedException("The server has not been locked.");
                }
            }
        }

        private bool IsLockObtainedBy(string lockIdentifier)
        {
            return string.Equals(_lock, lockIdentifier, StringComparison.Ordinal);
        }

        public override IRequestInterception Intercept(string commandIdentifier, string lockIdentifier)
        {
            lock (this)
            {
                if (!IsLocked || !IsLockObtainedBy(lockIdentifier))
                {
                    throw new InvalidLockIdentifierException($"The lock {lockIdentifier} has been released or is expired.");
                }
                return null;
            }
        }
    }
}

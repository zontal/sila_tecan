using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.DynamicClient;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Recovery
{
    /// <summary>
    /// Denotes the default implementation of a recovery service
    /// </summary>
    [Export( typeof( IErrorRecoveryService ) )]
    [Export( typeof( IRecoveryService ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    public class ErrorRecoveryService : IErrorRecoveryService, IRecoveryService, INotifyPropertyChanged
    {
        private readonly ConcurrentDictionary<string, RecoverableErrorInternal> _errors = new ConcurrentDictionary<string, RecoverableErrorInternal>();
        private readonly List<IRecoveryStrategy> _strategies;
        private readonly ISiLAServer _server;
        private readonly Timer _timer;
        private TimeSpan? _recoveryTimeout = TimeSpan.FromMinutes( 5 );

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="strategies">The strategies to support</param>
        /// <param name="server">The server</param>
        [ImportingConstructor]
        public ErrorRecoveryService( [ImportMany] IEnumerable<IRecoveryStrategy> strategies, ISiLAServer server )
        {
            _strategies = strategies?.ToList();
            _server = server;
            _timer = new Timer( Cleanup );
        }

        private void Cleanup( object state )
        {
            foreach(var error in _errors.Values)
            {
                if(error.Expiry.HasValue && error.Expiry.Value < DateTime.Now)
                {
                    try
                    {
                        error.Recover( error.DefaultOption, null );
                        _errors.TryRemove( error.Command.CommandExecutionID, out _ );
                    }
                    catch
                    {
                        error.Abort();
                    }
                }
            }
        }

        /// <inheritdoc />
        public ICollection<RecoverableError> RecoverableErrors => _errors.Values.Select( e => e.AsRecoverableError() ).ToList();

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public void AbortErrorHandling( string commandExecutionUUID )
        {
            if(_errors.TryRemove( commandExecutionUUID, out var recoverableError ))
            {
                recoverableError.Abort();
                DisableTimerIfNotNeeded();
            }
            else
            {
                throw new InvalidCommandExecutionUUIDException( "There is no error to recover" );
            }
        }

        private void DisableTimerIfNotNeeded()
        {
            if(_errors.Count == 0)
            {
                _timer.Change( System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite );
            }
        }

        /// <inheritdoc />
        public void ExecuteContinuationOption( string commandExecutionUUID, string continuationOption, DynamicObjectProperty inputData )
        {
            if(_errors.TryRemove( commandExecutionUUID, out var recoverableError ))
            {
                try
                {
                    recoverableError.Recover( continuationOption, inputData?.Value );
                    DisableTimerIfNotNeeded();
                }
                catch(Exception)
                {
                    _errors.TryAdd( commandExecutionUUID, recoverableError );
                    throw;
                }
            }
            else
            {
                throw new InvalidCommandExecutionUUIDException( "There is no error to recover" );
            }
        }

        /// <inheritdoc />
        public Task<RecoveryResult> Recover( Exception exception, IReadOnlyDictionary<string, string> context, TimeSpan? duration, int attempt )
        {
            ObservableCommandExecution execution = FindExecution( context );
            if(execution == null)
            {
                return Task.FromResult( RecoveryResult.Throw );
            }
            var strategies = _strategies.Where( s => s.CanRecover( exception, context, attempt ) ).ToList();
            if(strategies.Count == 0)
            {
                return Task.FromResult( RecoveryResult.Throw );
            }
            var timeout = duration ?? _recoveryTimeout;
            var defaultStrategy = strategies.OrderByDescending( s => s.Priority ).First();
            var recoverableError = new RecoverableErrorInternal( strategies, defaultStrategy.Identifier, timeout.HasValue ? DateTime.Now + timeout.Value : null, execution, context, exception );
            lock(_errors)
            {
                if(_errors.Count == 0)
                {
                    _timer.Change( 0, 1000 );
                }
                _errors.TryAdd( execution.CommandExecutionID, recoverableError );
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( RecoverableErrors ) ) );
            }
            return recoverableError.RecoveryTask;
        }

        private ObservableCommandExecution FindExecution( IReadOnlyDictionary<string, string> context )
        {
            if(context != null && context.TryGetValue( nameof( IServerCommand.CommandExecutionUuid ), out var executionUuid ))
            {
                return _server.GetCommandExecution( executionUuid );
            }
            // if no command execution is contained in the context but there is a running observable command, take it
            return _server.FindCommandExecutions( state => state == CommandState.Running ).FirstOrDefault();
        }

        /// <inheritdoc />
        public void SetErrorHandlingTimeout( Timeout errorHandlingTimeout )
        {
            if(errorHandlingTimeout.Value > 0)
            {
                _recoveryTimeout = TimeSpan.FromSeconds( errorHandlingTimeout.Value );
            }
            else
            {
                _recoveryTimeout = null;
            }
        }
    }
}

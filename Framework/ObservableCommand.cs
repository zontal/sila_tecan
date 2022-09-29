using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes the base class for an observable command
    /// </summary>
    public abstract class ObservableCommand : IObservableCommand, IDisposable
    {
        private readonly Channel<StateUpdate> _stateUpdateChannel;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private StateUpdate _stateUpdate;

        /// <summary>
        /// Creates an observable command
        /// </summary>
        protected ObservableCommand() : this( null ) { }

        /// <summary>
        /// Creates an observable command
        /// </summary>
        protected ObservableCommand( CancellationTokenSource cancellationTokenSource )
        {
            _stateUpdateChannel = Channels.CreateStateUpdateChannel();
            _cancellationTokenSource = cancellationTokenSource;
        }

        /// <inheritdoc />
        public virtual void Start()
        {
            if(Response != null)
            {
                throw new InvalidOperationException( "The command was already started" );
            }
            Response = WrapRun();
        }

        /// <summary>
        /// Runs the command in a derived class
        /// </summary>
        /// <returns>The command main response task</returns>
        public abstract Task Run();


        private async Task WrapRun()
        {
            Exception exception = null;
            try
            {
                await Run();
            }
            catch(Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                if(SendLastUpdate)
                {
                    PushStateUpdate( 1, TimeSpan.Zero, exception != null ? CommandState.FinishedWithErrors : CommandState.FinishedSuccess );
                }

                Cleanup( exception );
            }
        }

        /// <summary>
        /// Gets a value indicating whether the command should implicitly send a state update after being finished
        /// </summary>
        protected virtual bool SendLastUpdate => true;

        /// <summary>
        /// Pushes an updated state
        /// </summary>
        /// <param name="progress">The percentage to which the command is finished, in a range from 0-1</param>
        /// <param name="remainingTime">An estimation of the remaining time</param>
        /// <param name="state">The current state of the command execution</param>
        /// <returns></returns>
        protected void PushStateUpdate( double progress, TimeSpan remainingTime, CommandState state )
        {
            PushStateUpdate( new StateUpdate( progress, remainingTime, state ) );
        }

        /// <summary>
        /// Pushes an updated state
        /// </summary>
        /// <param name="update">The state update</param>
        /// <returns></returns>
        protected void PushStateUpdate( StateUpdate update )
        {
            _stateUpdateChannel.Writer.TryWrite( update );
            _stateUpdate = update;
        }

        /// <summary>
        /// Cleans up resources used for the execution of this command
        /// </summary>
        /// <param name="exception">An exception that caused the command to terminate or null, if the command completed successfully</param>
        protected virtual void Cleanup( Exception exception )
        {
            _stateUpdateChannel.Writer.TryComplete();
            _cancellationTokenSource?.Dispose();
        }

        /// <inheritdoc />
        public ChannelReader<StateUpdate> StateUpdates => _stateUpdateChannel.Reader;

        /// <inheritdoc />
        public Task Response
        {
            get;
            private set;
        }

        /// <inheritdoc />
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        /// <inheritdoc />
        public bool IsCancellationSupported => _cancellationTokenSource != null;

        /// <inheritdoc />
        public bool IsStarted => Response != null;

        /// <inheritdoc />
        public StateUpdate State => _stateUpdate;

        /// <inheritdoc />
        public virtual void Cancel()
        {
            if(_cancellationTokenSource != null)
            {
                try
                {
                    _cancellationTokenSource.Cancel();
                }
                catch(ObjectDisposedException)
                {
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Cleanup( null );
        }
    }

    /// <summary>
    /// Denotes the base class for an observable command
    /// </summary>
    public abstract class ObservableCommand<T> : IObservableCommand<T>, IDisposable
    {
        private readonly Channel<StateUpdate> _stateUpdateChannel;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private StateUpdate _stateUpdate;

        /// <summary>
        /// Creates an observable command
        /// </summary>
        protected ObservableCommand() : this( null ) { }

        /// <summary>
        /// Creates an observable command
        /// </summary>
        protected ObservableCommand( CancellationTokenSource cancellationTokenSource )
        {
            _stateUpdateChannel = Channels.CreateStateUpdateChannel();
            _cancellationTokenSource = cancellationTokenSource;
        }

        /// <inheritdoc />
        public virtual void Start()
        {
            Response = WrapRun();
        }

        /// <summary>
        /// Runs the command in a derived class
        /// </summary>
        /// <returns>The command main response task</returns>
        public abstract Task<T> Run();

        private async Task<T> WrapRun()
        {
            Exception exception = null;
            try
            {
                return await Run();
            }
            catch(Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                if(SendLastUpdate)
                {
                    PushStateUpdate( 1, TimeSpan.Zero, exception != null ? CommandState.FinishedWithErrors : CommandState.FinishedSuccess );
                }

                Cleanup( exception );
            }
        }

        /// <summary>
        /// Gets a value indicating whether the command should implicitly send a state update after being finished
        /// </summary>
        protected virtual bool SendLastUpdate => true;

        /// <summary>
        /// Cleans up resources used for the execution of this command
        /// </summary>
        /// <param name="exception">An exception that caused the command to terminate or null, if the command completed successfully</param>
        protected virtual void Cleanup( Exception exception )
        {
            _stateUpdateChannel.Writer.TryComplete();
            _cancellationTokenSource?.Dispose();
        }

        /// <summary>
        /// Pushes an updated state
        /// </summary>
        /// <param name="progress">The percentage to which the command is finished, in a range from 0-1</param>
        /// <param name="remainingTime">An estimation of the remaining time</param>
        /// <param name="state">The current state of the command execution</param>
        /// <returns></returns>
        protected void PushStateUpdate( double progress, TimeSpan remainingTime, CommandState state )
        {
            var update = new StateUpdate( progress, remainingTime, state );
            _stateUpdateChannel.Writer.TryWrite( update );
            _stateUpdate = update;
        }

        /// <inheritdoc />
        public ChannelReader<StateUpdate> StateUpdates => _stateUpdateChannel.Reader;

        /// <inheritdoc />
        public Task<T> Response
        {
            get;
            private set;
        }

        Task IObservableCommand.Response => Response;


        /// <inheritdoc />
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        /// <inheritdoc />
        public bool IsCancellationSupported => _cancellationTokenSource != null;

        /// <inheritdoc />
        public bool IsStarted => Response != null;

        /// <inheritdoc />
        public StateUpdate State => _stateUpdate;

        /// <inheritdoc />
        public virtual void Cancel()
        {
            if(_cancellationTokenSource != null)
            {
                try
                {
                    _cancellationTokenSource.Cancel();
                }
                catch(ObjectDisposedException)
                {
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Cleanup( null );
        }
    }
}

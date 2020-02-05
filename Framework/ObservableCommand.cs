using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes the base class for an observable command
    /// </summary>
    public class ObservableCommand : IObservableCommand
    {
        private readonly Channel<StateUpdate> _stateUpdateChannel;

        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <param name="command">The actual command method</param>
        public ObservableCommand( Action<Action<StateUpdate>> command ) : this(ConvertCommandHandler(command))
        {
        }

        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <param name="command">The actual command method</param>
        public ObservableCommand(Func<Func<StateUpdate, ValueTask>, Task> command)
        {
            _stateUpdateChannel = Channels.CreateStateUpdateChannel();
            Response = StartCommand(command);
        }

        private static Func<Func<StateUpdate, ValueTask>, Task> ConvertCommandHandler( Action<Action<StateUpdate>> command )
        {
            return updateHandler =>
            {
                var newUpdateHandler = new Action<StateUpdate>(update => updateHandler(update).AsTask().Wait());
                return Task.Factory.StartNew( () => command( newUpdateHandler ) );
            };
        }

        private async Task StartCommand( Func<Func<StateUpdate, ValueTask>, Task> command )
        {
            Exception exception = null;
            try
            {
                await command( PushStateUpdate );
            }
            catch( Exception ex )
            {
                exception = ex;
                throw;
            }
            finally
            {
                await PushStateUpdate( new StateUpdate( 1, TimeSpan.Zero, exception != null ? CommandState.FinishedWithErrors : CommandState.FinishedSuccess ) );
                _stateUpdateChannel.Writer.Complete( exception );
            }
        }

        private ValueTask PushStateUpdate( StateUpdate stateUpdate )
        {
            return _stateUpdateChannel.Writer.WriteAsync( stateUpdate );
        }

        /// <inheritdoc />
        public ChannelReader<StateUpdate> StateUpdates => _stateUpdateChannel.Reader;

        /// <inheritdoc />
        public Task Response
        {
            get;
        }
    }

    /// <summary>
    /// Denotes the base class for an observable command
    /// </summary>
    public class ObservableCommand<T> : IObservableCommand<T>
    {
        private readonly Channel<StateUpdate> _stateUpdateChannel;

        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <param name="command">The actual command method</param>
        public ObservableCommand( Func<Action<StateUpdate>, T> command ) : this( ConvertCommandHandler( command ) )
        {
        }

        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <param name="command">The actual command method</param>
        public ObservableCommand( Func<Func<StateUpdate, ValueTask>, Task<T>> command )
        {
            _stateUpdateChannel = Channels.CreateStateUpdateChannel();
            Response = StartCommand( command );
        }

        private static Func<Func<StateUpdate, ValueTask>, Task<T>> ConvertCommandHandler( Func<Action<StateUpdate>, T> command )
        {
            return updateHandler =>
            {
                var newUpdateHandler = new Action<StateUpdate>( update => updateHandler( update ).AsTask().Wait() );
                return Task.Factory.StartNew( () => command( newUpdateHandler ) );
            };
        }

        private async Task<T> StartCommand( Func<Func<StateUpdate, ValueTask>, Task<T>> command )
        {
            Exception exception = null;
            try
            {
                return await command( PushStateUpdate );
            }
            catch(Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                await PushStateUpdate( new StateUpdate( 1, TimeSpan.Zero, exception != null ? CommandState.FinishedWithErrors : CommandState.FinishedSuccess ) );
                _stateUpdateChannel.Writer.Complete( exception );
            }
        }

        private ValueTask PushStateUpdate( StateUpdate stateUpdate )
        {
            return _stateUpdateChannel.Writer.WriteAsync( stateUpdate );
        }

        /// <inheritdoc />
        public ChannelReader<StateUpdate> StateUpdates => _stateUpdateChannel.Reader;

        /// <inheritdoc />
        public Task<T> Response
        {
            get;
        }

        Task IObservableCommand.Response => Response;
    }
}

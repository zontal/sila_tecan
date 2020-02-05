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
    public class ObservableIntermediatesCommand<TIntermediate> : IIntermediateObservableCommand<TIntermediate>
    {
        private readonly Channel<StateUpdate> _stateUpdateChannel;
        private readonly Channel<TIntermediate> _intermediatesChannel;

        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <param name="command">The actual command method</param>
        public ObservableIntermediatesCommand( Action<Action<StateUpdate>, Action<TIntermediate>> command ) : this( ConvertCommandHandler( command ) )
        {
        }

        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <param name="command">The actual command method</param>
        public ObservableIntermediatesCommand( Func<Func<StateUpdate, ValueTask>, Func<TIntermediate, ValueTask>, Task> command )
        {
            _stateUpdateChannel = Channels.CreateStateUpdateChannel();
            _intermediatesChannel = Channels.CreateIntermediateChannel<TIntermediate>();
            Response = StartCommand( command );
        }

        private static Func<Func<StateUpdate, ValueTask>, Func<TIntermediate, ValueTask>, Task> ConvertCommandHandler( Action<Action<StateUpdate>, Action<TIntermediate>> command )
        {
            return (updateHandler, intermediateHandler) =>
            {
                var newUpdateHandler = new Action<StateUpdate>( update => updateHandler( update ).AsTask().Wait() );
                var newIntermediateHandler = new Action<TIntermediate>( intermediate => intermediateHandler( intermediate ).AsTask().Wait() );
                return Task.Factory.StartNew( () => command( newUpdateHandler, newIntermediateHandler ) );
            };
        }

        private async Task StartCommand( Func<Func<StateUpdate, ValueTask>, Func<TIntermediate, ValueTask>, Task> command )
        {
            Exception exception = null;
            try
            {
                await command( PushStateUpdate, PushIntermediate );
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
                _intermediatesChannel.Writer.Complete( exception );
            }
        }

        private ValueTask PushStateUpdate( StateUpdate stateUpdate )
        {
            return _stateUpdateChannel.Writer.WriteAsync( stateUpdate );
        }

        private ValueTask PushIntermediate( TIntermediate intermediate )
        {
            return _intermediatesChannel.Writer.WriteAsync( intermediate );
        }

        /// <inheritdoc />
        public ChannelReader<StateUpdate> StateUpdates => _stateUpdateChannel.Reader;

        /// <inheritdoc />
        public ChannelReader<TIntermediate> IntermediateValues => _intermediatesChannel.Reader;

        /// <inheritdoc />
        public Task Response
        {
            get;
        }
    }

    /// <summary>
    /// Denotes the base class for an observable command
    /// </summary>
    public class ObservableIntermediatesCommand<TIntermediate, T> : IIntermediateObservableCommand<TIntermediate, T>
    {
        private readonly Channel<StateUpdate> _stateUpdateChannel;
        private readonly Channel<TIntermediate> _intermediatesChannel;

        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <param name="command">The actual command method</param>
        public ObservableIntermediatesCommand( Func<Action<StateUpdate>, Action<TIntermediate>, T> command ) : this( ConvertCommandHandler( command ) )
        {
        }

        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <param name="command">The actual command method</param>
        public ObservableIntermediatesCommand( Func<Func<StateUpdate, ValueTask>, Func<TIntermediate, ValueTask>, Task<T>> command )
        {
            _stateUpdateChannel = Channels.CreateStateUpdateChannel();
            _intermediatesChannel = Channels.CreateIntermediateChannel<TIntermediate>();
            Response = StartCommand( command );
        }

        private static Func<Func<StateUpdate, ValueTask>, Func<TIntermediate, ValueTask>, Task<T>> ConvertCommandHandler( Func<Action<StateUpdate>, Action<TIntermediate>, T> command )
        {
            return (updateHandler, intermediateHandler) =>
            {
                var newUpdateHandler = new Action<StateUpdate>( update => updateHandler( update ).AsTask().Wait() );
                var newIntermediateHandler = new Action<TIntermediate>(intermediate => intermediateHandler(intermediate).AsTask().Wait());
                return Task.Factory.StartNew( () => command( newUpdateHandler, newIntermediateHandler ) );
            };
        }

        private async Task<T> StartCommand( Func<Func<StateUpdate, ValueTask>, Func<TIntermediate, ValueTask>, Task<T>> command )
        {
            Exception exception = null;
            try
            {
                return await command( PushStateUpdate, PushIntermediate );
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
                _intermediatesChannel.Writer.Complete( exception );
            }
        }

        private ValueTask PushStateUpdate( StateUpdate stateUpdate )
        {
            return _stateUpdateChannel.Writer.WriteAsync( stateUpdate );
        }

        private ValueTask PushIntermediate( TIntermediate intermediate )
        {
            return _intermediatesChannel.Writer.WriteAsync( intermediate );
        }

        /// <inheritdoc />
        public ChannelReader<StateUpdate> StateUpdates => _stateUpdateChannel.Reader;

        /// <inheritdoc />
        public ChannelReader<TIntermediate> IntermediateValues => _intermediatesChannel.Reader;

        /// <inheritdoc />
        public Task<T> Response
        {
            get;
        }

        Task IObservableCommand.Response => Response;
    }
}

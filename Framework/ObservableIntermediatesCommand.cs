using System;
using System.Threading;
using System.Threading.Channels;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes the base class for an observable command
    /// </summary>
    public abstract class ObservableIntermediatesCommand<TIntermediate> : ObservableCommand, IIntermediateObservableCommand<TIntermediate>
    {
        private readonly Channel<TIntermediate> _intermediatesChannel;


        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <param name="cancellationTokenSource">A cancellation token source</param>
        protected ObservableIntermediatesCommand( CancellationTokenSource cancellationTokenSource ) : base( cancellationTokenSource )
        {
            _intermediatesChannel = Channels.CreateIntermediateChannel<TIntermediate>();
        }

        /// <summary>
        /// Creates an observable command
        /// </summary>
        protected ObservableIntermediatesCommand()
        {
            _intermediatesChannel = Channels.CreateIntermediateChannel<TIntermediate>();
        }

        /// <summary>
        /// Pushes the provided intermediate value
        /// </summary>
        /// <param name="intermediate">The intermediate value</param>
        protected void PushIntermediate( TIntermediate intermediate )
        {
            _intermediatesChannel.Writer.TryWrite( intermediate );
        }

        /// <inheritdoc />
        protected override void Cleanup( Exception exception )
        {
            base.Cleanup( exception );
            _intermediatesChannel.Writer.TryComplete();
        }

        /// <inheritdoc />
        public ChannelReader<TIntermediate> IntermediateValues => _intermediatesChannel.Reader;
    }

    /// <summary>
    /// Denotes the base class for an observable command
    /// </summary>
    public abstract class ObservableIntermediatesCommand<TIntermediate, T> : ObservableCommand<T>, IIntermediateObservableCommand<TIntermediate, T>
    {
        private readonly Channel<TIntermediate> _intermediatesChannel;


        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <param name="cancellationTokenSource">A cancellation token source</param>
        protected ObservableIntermediatesCommand( CancellationTokenSource cancellationTokenSource ) : base( cancellationTokenSource )
        {
            _intermediatesChannel = Channels.CreateIntermediateChannel<TIntermediate>();
        }

        /// <summary>
        /// Creates an observable command
        /// </summary>
        protected ObservableIntermediatesCommand()
        {
            _intermediatesChannel = Channels.CreateIntermediateChannel<TIntermediate>();
        }

        /// <summary>
        /// Pushes the provided intermediate value
        /// </summary>
        /// <param name="intermediate">The intermediate value</param>
        protected void PushIntermediate( TIntermediate intermediate )
        {
            _intermediatesChannel.Writer.TryWrite( intermediate );
        }

        /// <inheritdoc />
        protected override void Cleanup( Exception exception )
        {
            base.Cleanup( exception );
            _intermediatesChannel.Writer.TryComplete();
        }

        /// <inheritdoc />
        public ChannelReader<TIntermediate> IntermediateValues => _intermediatesChannel.Reader;
    }
}

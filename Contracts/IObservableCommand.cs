using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Tecan.Sila2
{
    /// <summary>
    /// Describes a command whose execution can be observed
    /// </summary>
    /// <typeparam name="TResponse">The response type of the command</typeparam>
    public interface IObservableCommand<TResponse> : IObservableCommand
    {
        /// <summary>
        /// Gets the response task of the observable command
        /// </summary>
        new Task<TResponse> Response { get; }
    }

    /// <summary>
    /// Describes a command whose execution can be observed
    /// </summary>
    public interface IObservableCommand
    {
        /// <summary>
        /// Gets the current state of the command
        /// </summary>
        StateUpdate State
        {
            get;
        }

        /// <summary>
        /// Gets a channel on which the command sends state updates
        /// </summary>
        ChannelReader<StateUpdate> StateUpdates
        {
            get;
        }

        /// <summary>
        /// Explicitly starts the command execution
        /// </summary>
        void Start();

        /// <summary>
        /// Gets a value indicating whether the command has started
        /// </summary>
        bool IsStarted
        {
            get;
        }

        /// <summary>
        /// Gets the underlying task for this observable command
        /// </summary>
        Task Response { get; }

        /// <summary>
        /// Gets a cancellation token to inform clients when the command was cancelled
        /// </summary>
        CancellationToken CancellationToken
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether cancellation is supported
        /// </summary>
        bool IsCancellationSupported
        {
            get;
        }

        /// <summary>
        /// Cancels the command
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// Describes a command whose execution can be observed and that produces an intermediate state
    /// </summary>
    /// <typeparam name="TIntermediate">The type of the intermediate state</typeparam>
    public interface IIntermediateObservableCommand<TIntermediate> : IObservableCommand
    {
        /// <summary>
        /// Gets a channel on which the command sends intermediate values
        /// </summary>
        ChannelReader<TIntermediate> IntermediateValues
        {
            get;
        }
    }

    /// <summary>
    /// Describes a command whose execution can be observed and that produces an intermediate state
    /// </summary>
    /// <typeparam name="TIntermediate">The type of the intermediate state</typeparam>
    /// <typeparam name="TResponse">The response type of the command</typeparam>
    public interface IIntermediateObservableCommand<TIntermediate, TResponse> : IObservableCommand<TResponse>, IIntermediateObservableCommand<TIntermediate>
    {

    }
}

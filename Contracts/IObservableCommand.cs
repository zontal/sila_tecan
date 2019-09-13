using System;
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
        /// Fires when a new state update is received
        /// </summary>
        event EventHandler<StateUpdateEventArgs> StateUpdateReceived;

        /// <summary>
        /// Gets the underlying task for this observable command
        /// </summary>
        Task Response { get; }
    }

    /// <summary>
    /// Describes a command whose execution can be observed and that produces an intermediate state
    /// </summary>
    /// <typeparam name="TIntermediate">The type of the intermediate state</typeparam>
    public interface IIntermediateObservableCommand<TIntermediate> : IObservableCommand
    {
        /// <summary>
        /// Gets fired when there are new intemediate results for the given command available
        /// </summary>
        event EventHandler<IntermediateResponseEventArgs<TIntermediate>> IntermediateResponse;
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

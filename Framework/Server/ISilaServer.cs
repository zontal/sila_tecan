using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Binary;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes an abstract SiLA2 server
    /// </summary>
    public interface ISiLAServer : IBinaryStore
    {
        /// <summary>
        /// Gets the features implemented by this server
        /// </summary>
        IEnumerable<Feature> Features { get; }

        /// <summary>
        /// Starts the server
        /// </summary>
        void StartServer();

        /// <summary>
        /// Shuts down the server
        /// </summary>
        /// <returns>Gets an awaitable that completes when the server is shut down</returns>
        Task ShutdownServer();

        /// <summary>
        /// Add a feature
        /// </summary>
        /// <param name="feature">A provider of a feature</param>
        void AddFeature(IFeatureProvider feature);

        /// <summary>
        /// Create a task to read the given property
        /// </summary>
        /// <typeparam name="T">The type of the property</typeparam>
        /// <param name="propertyIdentifier">The fully qualified identifier of the property</param>
        /// <param name="value">The current property value</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>A task that returns the property value</returns>
        Task<T> CreatePropertyTask<T>(string propertyIdentifier, T value, ServerCallContext context);

        /// <summary>
        /// Create a task to invoke a command
        /// </summary>
        /// <typeparam name="TResponse">The response type of the command</typeparam>
        /// <param name="command">The actual command</param>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>A task that returns the command result</returns>
        Task<TResponse> InvokeCommand<TResponse>(Func<Task<TResponse>> command, string commandIdentifier,
            ServerCallContext context);

        /// <summary>
        /// Create a task to invoke a command
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type of the command</typeparam>
        /// <param name="command">The actual command</param>
        /// <param name="request">The request object</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>A task that returns the command result</returns>
        Task<TResponse> InvokeCommand<TRequest, TResponse>(Func<TRequest, TResponse> command, TRequest request, ServerCallContext context)
            where TRequest : ISilaRequestObject;

        /// <summary>
        /// Adds an observable command execution
        /// </summary>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>The current state of the execution</returns>
        CommandExecutionInfo AddObservableCommandExecution(string commandIdentifier, ServerCallContext context);

        /// <summary>
        /// Adds an observable command execution
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="context">The context in which the call is made</param>
        /// <param name="resultFactory">Method to obtain the result</param>
        /// <returns>The current state of the execution</returns>
        CommandExecutionInfo AddObservableCommandExecution<T>(string commandIdentifier, Func<T> resultFactory, ServerCallContext context);

        /// <summary>
        /// Adds an observable command execution
        /// </summary>
        /// <typeparam name="TIntermediate">The type of the intermediate values</typeparam>
        /// <param name="intermediateValueGenerator">An object that provides intermediate values</param>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>The current state of the execution</returns>
        CommandExecutionInfo AddObservableCommandExecution<TIntermediate>(string commandIdentifier, IValueGenerator<TIntermediate> intermediateValueGenerator, ServerCallContext context);

        /// <summary>
        /// Adds an observable command execution
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <typeparam name="TIntermediate">The type of the intermediate values</typeparam>
        /// <param name="intermediateValueGenerator">An object that provides intermediate values</param>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="resultFactory">Method to obtain the result</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>The current state of the execution</returns>
        CommandExecutionInfo AddObservableCommandExecution<TIntermediate, T>(string commandIdentifier, IValueGenerator<TIntermediate> intermediateValueGenerator, Func<T> resultFactory, ServerCallContext context);
        
        /// <summary>
        /// Gets the execution state of the given command execution identifier
        /// </summary>
        /// <param name="commandExecutionID">The identifier of the command execution</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>The execution state</returns>
        ExecutionState GetObservableCommandExecutionState(string commandExecutionID, ServerCallContext context);

        /// <summary>
        /// Gets the execution state of the given command execution identifier
        /// </summary>
        /// <typeparam name="T">The command response type</typeparam>
        /// <param name="commandExecutionID">The identifier of the command execution</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>The execution state and a function to get the result</returns>
        (ExecutionState state, Func<T> result) GetObservableCommandExecutionState<T>(string commandExecutionID, ServerCallContext context);

        /// <summary>
        /// Gets the intermediate value generator for the given command execution identifier
        /// </summary>
        /// <typeparam name="TIntermediate">The type of the intermediate results</typeparam>
        /// <param name="commandExecutionID">The command execution identifier</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>The intermediate value generator</returns>
        IValueGenerator<TIntermediate> GetIntermediateValueGenerator<TIntermediate>(string commandExecutionID, ServerCallContext context);
    }

    /// <summary>
    /// Denotes the current state of an observable command execution
    /// </summary>
    public struct CommandExecutionInfo
    {
        /// <summary>
        /// Gets the execution identifier
        /// </summary>
        public string ExecutionUuid { get; }

        /// <summary>
        /// Gets the current state of the execution
        /// </summary>
        public ExecutionState State { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="executionUuid">the execution identifier</param>
        /// <param name="state">the current state of the execution</param>
        public CommandExecutionInfo(string executionUuid, ExecutionState state) : this()
        {
            ExecutionUuid = executionUuid;
            State = state;
        }
    }

    /// <summary>
    /// Denotes an interface for an object that raises data
    /// </summary>
    /// <typeparam name="T">the type of the data</typeparam>
    public interface IValueGenerator<T>
    {
        /// <summary>
        /// Raised when new data arrives
        /// </summary>
        event Action<T> NewData;
    }
}

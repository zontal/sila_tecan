using System;
using System.Collections.Generic;
using System.Threading.Channels;
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
        IEnumerable<IFeatureProvider> Features { get; }

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
        /// <param name="command">The command that should be executed</param>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>The current state of the execution</returns>
        string AddObservableCommandExecution(string commandIdentifier, IObservableCommand command, ServerCallContext context);
        
        /// <summary>
        /// Gets the execution state of the given command execution identifier
        /// </summary>
        /// <param name="commandExecutionID">The identifier of the command execution</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>The execution state</returns>
        CommandExecutionInfo GetObservableCommandExecutionState(string commandExecutionID, ServerCallContext context);

        /// <summary>
        /// Gets the intermediate channel for the given command execution id
        /// </summary>
        /// <typeparam name="TIntermediate">The type of intermediate results</typeparam>
        /// <param name="commandExecutionID">The command execution identifier</param>
        /// <param name="context">The context in which the call is made</param>
        ChannelReader<TIntermediate> GetIntermediateChannel<TIntermediate>( string commandExecutionID, ServerCallContext context );
    }

    /// <summary>
    /// Denotes the server information of an observable command execution
    /// </summary>
    public struct CommandExecutionInfo
    {
        /// <summary>
        /// Gets the state channel 
        /// </summary>
        public ChannelReader<StateUpdate> StateChannel
        {
            get;
        }

        /// <summary>
        /// Gets the task for the long-running observable command
        /// </summary>
        public Task ResultTask
        {
            get;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="stateChannel">The state channel</param>
        /// <param name="resultTask">The result task</param>
        public CommandExecutionInfo( ChannelReader<StateUpdate> stateChannel, Task resultTask )
        {
            StateChannel = stateChannel;
            ResultTask = resultTask;
        }
    }
}

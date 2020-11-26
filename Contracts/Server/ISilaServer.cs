using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

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
        /// Gets the command execution registered for the given command execution id
        /// </summary>
        /// <param name="commandExecutionId">The identifier of the command execution</param>
        /// <returns>The command execution object</returns>
        IObservableCommand GetCommandExecution( string commandExecutionId );

        /// <summary>
        /// Gets a collection of currently running commands that match the given filter
        /// </summary>
        /// <param name="stateFilter">The state filter or null to return all command executions</param>
        IObservableCommand[] FindCommandExecutions( Predicate<CommandState> stateFilter );

        /// <summary>
        /// Gets a component for the creation of error messages
        /// </summary>
        IServerErrorHandling ErrorHandling { get; }
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

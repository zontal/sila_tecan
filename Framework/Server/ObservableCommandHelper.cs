using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Helper class for observable commands
    /// </summary>
    public static class ObservableCommandHelper
    {
        /// <summary>
        /// Queries the command state for the given command execution identifier
        /// </summary>
        /// <param name="server">The server that is connected</param>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="responseStream">The stream to which responses should be written to</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>An awaitable task that completes when the client disconnects</returns>
        public static async Task QueryCommandState(ISiLAServer server, CommandExecutionUuid commandId,
            IServerStreamWriter<ExecutionState> responseStream, ServerCallContext context)
        {
            var executionState = server.GetObservableCommandExecutionState(commandId.CommandId, context);

            while( await executionState.StateChannel.WaitToReadAsync(context.CancellationToken) )
            {
                if( executionState.StateChannel.TryRead( out var currentState ) )
                {
                    await responseStream.WriteAsync( ConvertStateUpdate(currentState) );
                }
            }
        }

        private static ExecutionState ConvertStateUpdate( StateUpdate stateUpdate )
        {
            var state = new ExecutionState()
            {
                EstimatedRemainingTime = new DurationDto( stateUpdate.EstimatedRemainingTime ),
                Progress = new RealDto( stateUpdate.Progress ),
                UpdatedLifetimeOfExecution = new DurationDto( TimeSpan.FromMinutes( 5 ) )
            };
            switch(stateUpdate.State)
            {
                case CommandState.FinishedSuccess:
                    state.Status = ExecutionState.CommandStatus.FinishedSuccessfully;
                    break;
                case CommandState.FinishedWithErrors:
                    state.Status = ExecutionState.CommandStatus.FinishedWithError;
                    break;
                case CommandState.NotStarted:
                    state.Status = ExecutionState.CommandStatus.Waiting;
                    break;
                case CommandState.Running:
                    state.Status = ExecutionState.CommandStatus.Running;
                    break;
            }

            return state;
        }

        /// <summary>
        /// Starts the given observable command
        /// </summary>
        /// <param name="server">The server that should be used to add the command</param>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="command">The command that should be added</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>A task that returns the command confirmation from the server</returns>
        public static Task<CommandConfirmation> StartCommand(ISiLAServer server, string commandIdentifier, IObservableCommand command, ServerCallContext context)
        {
            var executionId = server.AddObservableCommandExecution(commandIdentifier, command, context);
            return Task.FromResult(new CommandConfirmation( executionId ) );
        }

        /// <summary>
        /// Wait until the command with the given command execution identifier is finished
        /// </summary>
        /// <param name="server">The server that should be connected</param>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>An awaitable task that completes when the command execution is done</returns>
        public static async Task<EmptyRequest> WaitCommandExecution(ISiLAServer server, CommandExecutionUuid commandId, ServerCallContext context)
        {
            var state = server.GetObservableCommandExecutionState(commandId.CommandId, context);
            await state.ResultTask;
            return EmptyRequest.Instance;
        }

        /// <summary>
        /// Wait until the command with the given command execution identifier is finished
        /// </summary>
        /// <param name="server">The server that should be connected</param>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="context">The context in which the call is made</param>
        /// <param name="converter">A function that specifies how to convert the response into a data transfer object</param>
        /// <returns>An awaitable task that completes when the command execution is done</returns>
        public static async Task<TResponseDto> WaitCommandExecution<TResponse, TResponseDto>(ISiLAServer server, CommandExecutionUuid commandId, ServerCallContext context, Func<TResponse, TResponseDto> converter)
        {
            var state = server.GetObservableCommandExecutionState(commandId.CommandId, context);
            var resultTask = state.ResultTask as Task<TResponse>;
            if( resultTask == null )
            {
                throw new ArgumentOutOfRangeException( nameof(commandId) );
            }

            if( converter == null )
            {
                throw new ArgumentNullException( nameof(converter) );
            }
            return converter( await resultTask );
        }

        /// <summary>
        /// Retrieves intermediate results for the command with the given command execution identifier
        /// </summary>
        /// <typeparam name="TIntermediate">The type of the intermediate responses</typeparam>
        /// <typeparam name="TIntermediateDto">The data transfer type of the the intermediate responses</typeparam>
        /// <param name="server">The server that is connected</param>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="responseStream">A stream of command responses</param>
        /// <param name="converter">A converter function to encapsulate intermediate values in data transfer objects</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>An awaitable task that completes when the command execution is done.</returns>
        public static async Task RetrieveIntermediateResults<TIntermediate, TIntermediateDto>(ISiLAServer server, CommandExecutionUuid commandId, IServerStreamWriter<TIntermediateDto> responseStream, Func<TIntermediate, TIntermediateDto> converter, ServerCallContext context)
        {
            var reader = server.GetIntermediateChannel<TIntermediate>( commandId.CommandId, context );

            while( await reader.WaitToReadAsync( context.CancellationToken ) )
            {
                if( reader.TryRead( out var intermediate ) )
                {
                    await responseStream.WriteAsync( converter( intermediate ) );
                }
            }
        }

        /// <summary>
        /// Sends the value from the provided value factory periodically to the client
        /// </summary>
        /// <typeparam name="T">The type of the data to be sent</typeparam>
        /// <param name="server">The server that should send the data</param>
        /// <param name="propertyIdentifier">The property identifier of the data</param>
        /// <param name="responseStream">The response stream to which the data should be written to</param>
        /// <param name="factory">A factory of the data</param>
        /// <param name="delay">A delay in which the data should be polled</param>
        /// <param name="context">The server context in which the request is made</param>
        /// <returns>A task that ends when the subscription by the client ends</returns>
        public static Task ContinuouslySendValue<T>(ISiLAServer server, string propertyIdentifier, IServerStreamWriter<T> responseStream, Func<T> factory,
            int delay, ServerCallContext context)
        {
            return server.InvokeCommand(() => SendValueAsync(responseStream, factory, delay, context), propertyIdentifier, context);
        }

        /// <summary>
        /// Sends the value from the provided value factory periodically to the client
        /// </summary>
        /// <typeparam name="T">The type of the data to be sent</typeparam>
        /// <param name="server">The server that should send the data</param>
        /// <param name="propertyIdentifier">The property identifier of the data</param>
        /// <param name="responseStream">The response stream to which the data should be written to</param>
        /// <param name="values">The values that should be sent</param>
        /// <param name="context">The server context in which the request is made</param>
        /// <returns>A task that ends when the subscription by the client ends</returns>
        public static Task ContinuouslySendValue<T>( ISiLAServer server, string propertyIdentifier, IServerStreamWriter<T> responseStream, IAsyncStreamReader<T> values, ServerCallContext context )
        {
            return server.InvokeCommand( () => SendValueAsync( responseStream, values, context ), propertyIdentifier, context );
        }

        private static async Task<int> SendValueAsync<T>(IServerStreamWriter<T> responseStream, Func<T> factory, int delay, ServerCallContext context)
        {
            do
            {
                await responseStream.WriteAsync(factory());
                await Task.Delay(delay);
            } while (!context.CancellationToken.IsCancellationRequested);
            return 0;
        }

        private static async Task<int> SendValueAsync<T>( IServerStreamWriter<T> responseStream, IAsyncStreamReader<T> values, ServerCallContext context )
        {
            while( await values.MoveNext( context.CancellationToken ) )
            {
                await responseStream.WriteAsync( values.Current );
            }

            return 0;
        }

        /// <summary>
        /// Subscribe to an observable property
        /// </summary>
        /// <typeparam name="T">The type of the data</typeparam>
        /// <param name="callInvoker">An object that performs the calls</param>
        /// <param name="subscribeMethod">The method to subscribe to values</param>
        /// <param name="newValueReceived">A method that is called when new data arrives</param>
        /// <param name="callInfo">Caller information for the property subscription</param>
        /// <returns>A task that ends when the subscription ends.</returns>
        public static Task SubscribeTo<T>( CallInvoker callInvoker, Method<EmptyRequest, T> subscribeMethod, Action<T> newValueReceived, IClientCallInfo callInfo )
            where T : class
        {
            var asyncCall = callInvoker.AsyncServerStreamingCall( subscribeMethod, null,
                callInfo.Options, EmptyRequest.Instance );
            var responseStream = asyncCall.ResponseStream;
            if( responseStream.MoveNext().Result )
            {
                newValueReceived?.Invoke( responseStream.Current );
                return ContinueReading( responseStream, newValueReceived, callInfo );
            }

            return Task.CompletedTask;
        }

        private static async Task ContinueReading<T>( IAsyncStreamReader<T> responseStream, Action<T> newValueReceived, IClientCallInfo callInfo ) where T : class
        {
            try
            {
                while(await responseStream.MoveNext())
                {
                    newValueReceived?.Invoke( responseStream.Current );
                }
                callInfo.FinishSuccessful();
            }
            catch( Exception exception )
            {
                callInfo.FinishWithErrors(exception);
                throw;
            }
        }
    }
}

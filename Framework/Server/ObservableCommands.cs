using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Helper class for observable commands
    /// </summary>
    public static class ObservableCommands
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
            
            while (executionState.Status == ExecutionState.CommandStatus.Waiting || executionState.Status == ExecutionState.CommandStatus.Running)
            {
                await responseStream.WriteAsync(executionState);
                await Task.Delay(500);
            }

            await responseStream.WriteAsync(executionState);
        }

        private static EventHandler<StateUpdateEventArgs> CreateCommandUpdateEventHandler(ExecutionState state)
        {
            return (o, e) =>
            {
                state.EstimatedRemainingTime = new DurationDto(e.EstimatedRemainingTime);
                state.Progress = new RealDto(e.Progress);
                switch (e.State)
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
            };
        }

        /// <summary>
        /// Starts the given observable command
        /// </summary>
        /// <typeparam name="TResult">The result type of the command</typeparam>
        /// <typeparam name="TResultResponse">The data transfer object type for the command response</typeparam>
        /// <param name="server">The server that should be used to add the command</param>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="command">The command that should be added</param>
        /// <param name="resultConverter">A method to convert the command response</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>A task that retuns the command confirmation from the server</returns>
        public static Task<CommandConfirmation> StartCommand<TResult, TResultResponse>(ISiLAServer server, string commandIdentifier, IObservableCommand<TResult> command, Func<TResult, TResultResponse> resultConverter, ServerCallContext context)
        {
            var executionInfo = server.AddObservableCommandExecution(commandIdentifier, () => resultConverter(command.Response.Result), context);
            command.StateUpdateReceived += CreateCommandUpdateEventHandler(executionInfo.State);
            return Task.FromResult(new CommandConfirmation(executionInfo.ExecutionUuid));
        }

        /// <summary>
        /// Starts the given observable command
        /// </summary>
        /// <param name="server">The server that should be used to add the command</param>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="command">The command that should be added</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>A task that retuns the command confirmation from the server</returns>
        public static Task<CommandConfirmation> StartCommand(ISiLAServer server, string commandIdentifier, IObservableCommand command, ServerCallContext context)
        {
            var executionInfo = server.AddObservableCommandExecution(commandIdentifier, context);
            command.StateUpdateReceived += CreateCommandUpdateEventHandler(executionInfo.State);
            return Task.FromResult(new CommandConfirmation(executionInfo.ExecutionUuid));
        }

        /// <summary>
        /// Starts the given observable command
        /// </summary>
        /// <typeparam name="TIntermediate">The intermediate type of the command</typeparam>
        /// <typeparam name="TIntermediateResponse">The data transfer object type for the command intermediate response</typeparam>
        /// <param name="intermediateConverter">A function to encapsulate the intermediate response</param>
        /// <param name="server">The server that should be used to add the command</param>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="command">The command that should be added</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>A task that retuns the command confirmation from the server</returns>
        public static Task<CommandConfirmation> StartCommand<TIntermediate, TIntermediateResponse>(ISiLAServer server, string commandIdentifier, IIntermediateObservableCommand<TIntermediate> command, Func<TIntermediate, TIntermediateResponse> intermediateConverter, ServerCallContext context)
        {
            var intermediateGenerator = new IntermediateValueGenerator<TIntermediate, TIntermediateResponse>(command, intermediateConverter);
            intermediateGenerator.Start();
            var executionInfo = server.AddObservableCommandExecution(commandIdentifier, intermediateGenerator, context);
            command.StateUpdateReceived += CreateCommandUpdateEventHandler(executionInfo.State);
            return Task.FromResult(new CommandConfirmation(executionInfo.ExecutionUuid));
        }

        /// <summary>
        /// Starts the given observable command
        /// </summary>
        /// <typeparam name="TResult">The result type of the command</typeparam>
        /// <typeparam name="TResultResponse">The data transfer object type for the command response</typeparam>
        /// <typeparam name="TIntermediate">The intermediate type of the command</typeparam>
        /// <typeparam name="TIntermediateResponse">The data transfer object type for the command intermediate response</typeparam>
        /// <param name="intermediateConverter">A function to encapsulate the intermediate response</param>
        /// <param name="server">The server that should be used to add the command</param>
        /// <param name="commandIdentifier">The fully qualified command identifier</param>
        /// <param name="command">The command that should be added</param>
        /// <param name="resultConverter">A method to convert the command response</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>A task that retuns the command confirmation from the server</returns>
        public static Task<CommandConfirmation> StartCommand<TIntermediate, TIntermediateResponse, TResult, TResultResponse>(ISiLAServer server, string commandIdentifier, IIntermediateObservableCommand<TIntermediate, TResult> command, Func<TIntermediate, TIntermediateResponse> intermediateConverter, Func<TResult, TResultResponse> resultConverter, ServerCallContext context)
        {
            var intermediateGenerator = new IntermediateValueGenerator<TIntermediate, TIntermediateResponse>(command, intermediateConverter);
            intermediateGenerator.Start();
            var executionInfo = server.AddObservableCommandExecution(commandIdentifier, intermediateGenerator, () => resultConverter(command.Response.Result), context);
            command.StateUpdateReceived += CreateCommandUpdateEventHandler(executionInfo.State);
            return Task.FromResult(new CommandConfirmation(executionInfo.ExecutionUuid));
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
            while (state.Status != ExecutionState.CommandStatus.FinishedSuccessfully && state.Status != ExecutionState.CommandStatus.FinishedWithError)
            {
                await Task.Delay(500);
            }

            return EmptyRequest.Instance;
        }

        /// <summary>
        /// Wait until the command with the given command execution identifier is finished
        /// </summary>
        /// <param name="server">The server that should be connected</param>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>An awaitable task that completes when the command execution is done</returns>
        public static async Task<TResponse> WaitCommandExecution<TResponse>(ISiLAServer server, CommandExecutionUuid commandId, ServerCallContext context)
        {
            var (state, result) = server.GetObservableCommandExecutionState<TResponse>(commandId.CommandId, context);
            while (state.Status != ExecutionState.CommandStatus.FinishedSuccessfully && state.Status != ExecutionState.CommandStatus.FinishedWithError)
            {
                await Task.Delay(500);
            }

            return result();
        }

        /// <summary>
        /// Retrieves intermediate results for the command with the given command execution identifier
        /// </summary>
        /// <typeparam name="TIntermediate">The type of the intermediate responses</typeparam>
        /// <param name="server">The server that is connected</param>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="responseStream">A stream of command responses</param>
        /// <param name="context">The context in which the call is made</param>
        /// <returns>An awaitable task that completes when the command execution is done.</returns>
        public static async Task RetrieveIntermediateResults<TIntermediate>(ISiLAServer server, CommandExecutionUuid commandId, IServerStreamWriter<TIntermediate> responseStream, ServerCallContext context)
        {
            var generator = server.GetIntermediateValueGenerator<TIntermediate>(commandId.CommandId, context);
            generator.NewData += data => responseStream.WriteAsync(data);
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(500);
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
        /// <returns>A task that ends when the subscription ends.</returns>
        public static async Task SubscribeTo<T>( CallInvoker callInvoker, Method<EmptyRequest, T> subscribeMethod, Action<T> newValueReceived )
            where T : class
        {
            var asyncCall = callInvoker.AsyncServerStreamingCall( subscribeMethod, null,
                new CallOptions(), EmptyRequest.Instance );
            var responseStream = asyncCall.ResponseStream;
            while( await responseStream.MoveNext() )
            {
                newValueReceived?.Invoke( responseStream.Current );
            }
        }
    }

    internal class IntermediateValueGenerator<TIntermediate, TIntermediateResponse> : IValueGenerator<TIntermediateResponse>
    {
        public Func<TIntermediate, TIntermediateResponse> Converter { get; }

        public IIntermediateObservableCommand<TIntermediate> Command { get; set; }

        public IntermediateValueGenerator(IIntermediateObservableCommand<TIntermediate> command, Func<TIntermediate, TIntermediateResponse> converter)
        {
            Command = command;
            Converter = converter;
        }

        public void Start()
        {
            Command.IntermediateResponse += Command_IntermediateResponse;
        }

        public void Stop()
        {
            Command.IntermediateResponse -= Command_IntermediateResponse;
        }

        private void Command_IntermediateResponse(object sender, IntermediateResponseEventArgs<TIntermediate> e)
        {
            NewData?.Invoke(Converter(e.Data));
        }

        public event Action<TIntermediateResponse> NewData;
    }
}

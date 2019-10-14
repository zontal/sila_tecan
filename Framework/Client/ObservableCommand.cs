using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Default implementation of an observable command via SiLA2
    /// </summary>
    public class ObservableCommand : IObservableCommand
    {
        /// <summary>
        /// The call info
        /// </summary>
        internal IClientCallInfo CallInfo { get; }

        /// <summary>
        /// The internal call invoker
        /// </summary>
        internal CallInvoker CallInvoker { get; }

        /// <summary>
        /// The command execution id
        /// </summary>
        public CommandExecutionUuid CommandId { get; }

        /// <summary>
        /// The method to obtain the command state
        /// </summary>
        internal Method<CommandExecutionUuid, ExecutionState> StateMethod { get; }

        /// <summary>
        /// Gets a function that can convert errors
        /// </summary>
        internal Func<DefinedExecutionErrorDto, Exception> ErrorConverter { get; }

        /// <summary>
        /// Create a new observable command
        /// </summary>
        /// <param name="callInfo">The call info</param>
        /// <param name="callInvoker">An invoker for gRPC</param>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="stateMethod">The state method</param>
        /// <param name="errorConverter">An error converter</param>
        internal ObservableCommand(IClientCallInfo callInfo, CallInvoker callInvoker, CommandExecutionUuid commandId, Method<CommandExecutionUuid, ExecutionState> stateMethod, Func<DefinedExecutionErrorDto, Exception> errorConverter)
        {
            CallInfo = callInfo;
            CallInvoker = callInvoker;
            CommandId = commandId;
            StateMethod = stateMethod;
        }

        internal virtual async Task Run()
        {
            try
            {
                await RunCore();
                CallInfo.FinishSuccessful();
            }
            catch (Exception exception)
            {
                var ex = ErrorHandling.ConvertException(exception, ErrorConverter);
                CallInfo.FinishWithErrors(exception);
                throw ex;
            }
        }

        internal async Task RunCore()
        {
            using (var streamingCall =
                                CallInvoker.AsyncServerStreamingCall(StateMethod, null, CallInfo.Options, CommandId))
            {
                var responseStream = streamingCall.ResponseStream;
                while (await responseStream.MoveNext())
                {
                    var state = ConvertState(responseStream.Current.Status);
                    var estimated = responseStream.Current.EstimatedRemainingTime.Extract(null);
                    var progress = responseStream.Current.Progress.Value;
                    StateUpdateReceived?.Invoke(this, new StateUpdateEventArgs(progress, estimated, state));
                }
            }
        }

        private static CommandState ConvertState(ExecutionState.CommandStatus state)
        {
            switch (state)
            {
                case ExecutionState.CommandStatus.Waiting:
                    return CommandState.NotStarted;
                case ExecutionState.CommandStatus.Running:
                    return CommandState.Running;
                case ExecutionState.CommandStatus.FinishedWithError:
                    return CommandState.FinishedWithErrors;
                case ExecutionState.CommandStatus.FinishedSuccessfully:
                    return CommandState.FinishedSuccess;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }


        /// <inheritdoc />
        public Task Response { get; protected set; }

        /// <inheritdoc />
        public event EventHandler<StateUpdateEventArgs> StateUpdateReceived;

        /// <summary>
        /// Starts the command
        /// </summary>
        protected virtual void Start()
        {
            Response = Run();
        }

        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <typeparam name="TRequest">The request data transfer type</typeparam>
        /// <param name="callInfo">The call info</param>
        /// <param name="callInvoker">An invoker for gRPC</param>
        /// <param name="request">The command parameters</param>
        /// <param name="method">The command method</param>
        /// <param name="stateMethod">The state method</param>
        /// <param name="errorConverter">An error converter</param>
        /// <returns>An observable command representing the executed command</returns>
        public static IObservableCommand Create<TRequest>(
            IClientCallInfo callInfo,
            CallInvoker callInvoker,
            TRequest request,
            Method<TRequest, CommandConfirmation> method,
            Method<CommandExecutionUuid, ExecutionState> stateMethod,
            Func<DefinedExecutionErrorDto, Exception> errorConverter)
            where TRequest : class, ISilaRequestObject
        {
            var confirmation = callInvoker.BlockingUnaryCall(method, null,
                callInfo.Options, request);
            var command = new ObservableCommand(callInfo, callInvoker, confirmation.CommandId, stateMethod, errorConverter);
            command.Start();
            return command;
        }

        /// <summary>
        /// Creates an observable command
        /// </summary>
        /// <typeparam name="TRequest">The request data transfer type</typeparam>
        /// <typeparam name="TResponse">The response data transfer type</typeparam>
        /// <typeparam name="TResult">The type of the </typeparam>
        /// <param name="callInfo">The call info</param>
        /// <param name="callInvoker">An invoker for gRPC</param>
        /// <param name="request">The command parameters</param>
        /// <param name="method">The command method</param>
        /// <param name="stateMethod">The state method</param>
        /// <param name="resultMethod">The method to obtain the result</param>
        /// <param name="resultConverter">A method to extract the result from the data transfer</param>
        /// <param name="errorConverter">An error converter</param>
        /// <returns>An observable command representing the executed command</returns>
        public static IObservableCommand<TResult> Create<TRequest, TResponse, TResult>(
            IClientCallInfo callInfo,
            CallInvoker callInvoker,
            TRequest request,
            Method<TRequest, CommandConfirmation> method,
            Method<CommandExecutionUuid, ExecutionState> stateMethod,
            Method<CommandExecutionUuid, TResponse> resultMethod,
            Func<TResponse, TResult> resultConverter,
            Func<DefinedExecutionErrorDto, Exception> errorConverter)
            where TRequest : class, ISilaRequestObject
            where TResponse : class
        {
            var confirmation = callInvoker.BlockingUnaryCall(method, null,
                new CallOptions(), request);
            var command = new ObservableCommand<TResponse, TResult>(callInfo, callInvoker, confirmation.CommandId, stateMethod, resultMethod, resultConverter, errorConverter);
            command.Start();
            return command;
        }

        /// <summary>
        /// Creates an observable command with intermediate results
        /// </summary>
        /// <typeparam name="TRequest">The request data transfer type</typeparam>
        /// <typeparam name="TIntermediateResponse">The data transfer object type for intermediate responses</typeparam>
        /// <typeparam name="TIntermediate">The type of intermediate results</typeparam>
        /// <param name="callInfo">The call info</param>
        /// <param name="callInvoker">An invoker for gRPC</param>
        /// <param name="request">The command parameters</param>
        /// <param name="method">The command method</param>
        /// <param name="stateMethod">The state method</param>
        /// <param name="intermediateMethod">The intermediate method</param>
        /// <param name="intermediateConverter">A function that extracts the intermediate result from its data transfer object</param>
        /// <param name="errorConverter">An error converter</param>
        /// <returns>An observable command representing the executed command</returns>
        public static IIntermediateObservableCommand<TIntermediate> CreateIntermediate<TRequest, TIntermediateResponse, TIntermediate>(
            IClientCallInfo callInfo,
            CallInvoker callInvoker,
            TRequest request,
            Method<TRequest, CommandConfirmation> method,
            Method<CommandExecutionUuid, ExecutionState> stateMethod,
            Method<CommandExecutionUuid, TIntermediateResponse> intermediateMethod,
            Func<TIntermediateResponse, TIntermediate> intermediateConverter,
            Func<DefinedExecutionErrorDto, Exception> errorConverter)
            where TRequest : class
            where TIntermediateResponse : class
        {
            var confirmation = callInvoker.BlockingUnaryCall(method, null,
                new CallOptions(), request);
            var command = new IntermediateObservableCommand<TIntermediateResponse, TIntermediate>(callInfo, callInvoker, confirmation.CommandId, stateMethod, intermediateMethod, intermediateConverter, errorConverter);
            command.Start();
            return command;
        }

        /// <summary>
        /// Creates an observable command with intermediate results
        /// </summary>
        /// <typeparam name="TRequest">The request data transfer type</typeparam>
        /// <typeparam name="TResponse">The response data transfer type</typeparam>
        /// <typeparam name="TResult">The type of the </typeparam>
        /// <typeparam name="TIntermediateResponse">The data transfer object type for intermediate responses</typeparam>
        /// <typeparam name="TIntermediate">The type of intermediate results</typeparam>
        /// <param name="callInfo">The call info</param>
        /// <param name="callInvoker">An invoker for gRPC</param>
        /// <param name="request">The command parameters</param>
        /// <param name="method">The command method</param>
        /// <param name="stateMethod">The state method</param>
        /// <param name="resultMethod">The method to obtain the result</param>
        /// <param name="intermediateMethod">The intermediate method</param>
        /// <param name="intermediateConverter">A function that extracts the intermediate result from its data transfer object</param>
        /// <param name="resultConverter">A method to extract the result from the data transfer</param>
        /// <param name="errorConverter">An error converter</param>
        /// <returns>An observable command representing the executed command</returns>
        public static IIntermediateObservableCommand<TIntermediate, TResult> CreateIntermediate<TRequest, TIntermediateResponse, TIntermediate, TResponse, TResult>(
            IClientCallInfo callInfo,
            CallInvoker callInvoker,
            TRequest request,
            Method<TRequest, CommandConfirmation> method,
            Method<CommandExecutionUuid, ExecutionState> stateMethod,
            Method<CommandExecutionUuid, TIntermediateResponse> intermediateMethod,
            Method<CommandExecutionUuid, TResponse> resultMethod,
            Func<TIntermediateResponse, TIntermediate> intermediateConverter,
            Func<TResponse, TResult> resultConverter,
            Func<DefinedExecutionErrorDto, Exception> errorConverter)
            where TRequest : class
            where TIntermediateResponse : class
            where TResponse : class
        {
            var confirmation = callInvoker.BlockingUnaryCall(method, null,
                new CallOptions(), request);
            var command = new IntermediateObservableCommand<TIntermediateResponse, TIntermediate, TResponse, TResult>(callInfo, callInvoker, confirmation.CommandId, stateMethod, intermediateMethod, resultMethod, intermediateConverter, resultConverter, errorConverter);
            command.Start();
            return command;
        }
    }

    internal class ObservableCommand<TResponse, TResult> : ObservableCommand, IObservableCommand<TResult>
        where TResponse : class
    {
        internal Method<CommandExecutionUuid, TResponse> ResultMethod { get; }
        internal Func<TResponse, TResult> ResultConverter { get; }

        /// <summary>
        /// Create a new observable command
        /// </summary>
        /// <param name="executionManager">The call info</param>
        /// <param name="callInvoker">An invoker for gRPC</param>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="stateMethod">The state method</param>
        /// <param name="errorConverter">An error converter</param>
        /// <param name="resultMethod">The method to obtain the result</param>
        /// <param name="resultConverter">A method to extract the result from the data transfer</param>
        public ObservableCommand(
            IClientCallInfo executionManager,
            CallInvoker callInvoker,
            CommandExecutionUuid commandId,
            Method<CommandExecutionUuid, ExecutionState> stateMethod,
            Method<CommandExecutionUuid, TResponse> resultMethod,
            Func<TResponse, TResult> resultConverter,
            Func<DefinedExecutionErrorDto, Exception> errorConverter)
            : base(executionManager, callInvoker, commandId, stateMethod, errorConverter)
        {
            ResultMethod = resultMethod;
            ResultConverter = resultConverter;
        }

        protected override void Start()
        {
            base.Response = RunAndGetResponse();
        }

        private async Task<TResult> RunAndGetResponse()
        {
            try
            {
                await RunCore();
                var resultCall = CallInvoker.AsyncUnaryCall(ResultMethod, null, new CallOptions(), CommandId);
                var result = ResultConverter(await resultCall.ResponseAsync);
                CallInfo.FinishSuccessful();
                return result;
            }
            catch (Exception exception)
            {
                var ex2 = ErrorHandling.ConvertException(exception, ErrorConverter);
                CallInfo.FinishWithErrors(ex2);
                throw ex2;
            }
        }

        public new Task<TResult> Response => base.Response as Task<TResult>;
    }

    internal class IntermediateObservableCommand<TIntermediateResponse, TIntermediate> : ObservableCommand, IIntermediateObservableCommand<TIntermediate>
        where TIntermediateResponse : class
    {
        public Func<TIntermediateResponse, TIntermediate> IntermediateConverter { get; }

        public Method<CommandExecutionUuid, TIntermediateResponse> IntermediateMethod { get; }


        public IntermediateObservableCommand(
            IClientCallInfo executionManager,
            CallInvoker callInvoker,
            CommandExecutionUuid commandId,
            Method<CommandExecutionUuid, ExecutionState> stateMethod,
            Method<CommandExecutionUuid, TIntermediateResponse> intermediateMethod,
            Func<TIntermediateResponse, TIntermediate> intermediateConverter,
            Func<DefinedExecutionErrorDto, Exception> errorConverter)
            : base(executionManager, callInvoker, commandId, stateMethod, errorConverter)
        {
            IntermediateConverter = intermediateConverter;
            IntermediateMethod = intermediateMethod;
        }

        protected override void Start()
        {
            Response = Run();
            FetchIntermediateResponses();
        }

        private protected async void FetchIntermediateResponses()
        {
            try
            {
                using (var streamingCall =
                    CallInvoker.AsyncServerStreamingCall(IntermediateMethod, null, CallInfo.Options, CommandId))
                {
                    var responseStream = streamingCall.ResponseStream;
                    while (await responseStream.MoveNext())
                    {
                        var intermediateResult = IntermediateConverter(responseStream.Current);
                        IntermediateResponse?.Invoke(this,
                            new IntermediateResponseEventArgs<TIntermediate>(intermediateResult));
                    }
                }
            }
            catch (Exception exception)
            {
                throw ErrorHandling.ConvertException(exception, ErrorConverter);
            }
        }

        public event EventHandler<IntermediateResponseEventArgs<TIntermediate>> IntermediateResponse;
    }

    internal class IntermediateObservableCommand<TIntermediateResponse, TIntermediate, TResultResponse, TResult> : IntermediateObservableCommand<TIntermediateResponse, TIntermediate>, IIntermediateObservableCommand<TIntermediate, TResult>
        where TIntermediateResponse : class
        where TResultResponse : class
    {
        public Method<CommandExecutionUuid, TResultResponse> ResultMethod { get; }
        public Func<TResultResponse, TResult> ResultConverter { get; }

        public IntermediateObservableCommand(
            IClientCallInfo callInfo,
            CallInvoker callInvoker,
            CommandExecutionUuid commandId,
            Method<CommandExecutionUuid, ExecutionState> stateMethod,
            Method<CommandExecutionUuid, TIntermediateResponse> intermediateMethod,
            Method<CommandExecutionUuid, TResultResponse> resultMethod,
            Func<TIntermediateResponse, TIntermediate> intermediateConverter,
            Func<TResultResponse, TResult> resultConverter,
            Func<DefinedExecutionErrorDto, Exception> errorConverter)
            : base(callInfo, callInvoker, commandId, stateMethod, intermediateMethod, intermediateConverter, errorConverter)
        {
            ResultMethod = resultMethod;
            ResultConverter = resultConverter;
        }

        protected override void Start()
        {
            base.Response = RunAndGetResponse();
            FetchIntermediateResponses();
        }

        private async Task<TResult> RunAndGetResponse()
        {
            try
            {
                await RunCore();
                var resultCall = CallInvoker.AsyncUnaryCall(ResultMethod, null, new CallOptions(), CommandId);
                var result = ResultConverter(await resultCall.ResponseAsync);
                CallInfo.FinishSuccessful();
                return result;
            }
            catch (Exception exception)
            {
                var ex2 = ErrorHandling.ConvertException(exception, ErrorConverter);
                CallInfo.FinishWithErrors(ex2);
                throw ex2;
            }
        }

        public new Task<TResult> Response => base.Response as Task<TResult>;
    }
}

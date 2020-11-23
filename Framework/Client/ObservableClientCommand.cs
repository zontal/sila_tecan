using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Channel = System.Threading.Channels.Channel;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Default implementation of an observable command via SiLA2
    /// </summary>
    internal class ObservableClientCommand : ObservableCommand, IClientCommand
    {
        public IClientCallInfo CallInfo { get; }

        public CallInvoker CallInvoker { get; }

        public CommandExecutionUuid CommandId { get; }

        public Method<CommandExecutionUuid, ExecutionState> StateMethod { get; }

        public Func<DefinedExecutionErrorDto, Exception> ErrorConverter { get; }

        public CallOptions CreateCallOptions() => CallInfo.ToCallOptions().WithCancellationToken( CancellationToken );

        protected override bool SendLastUpdate => false;

        public ObservableClientCommand(IClientCallInfo callInfo, CallInvoker callInvoker, CommandExecutionUuid commandId, Method<CommandExecutionUuid, ExecutionState> stateMethod, Func<DefinedExecutionErrorDto, Exception> errorConverter) : base(new CancellationTokenSource())
        {
            CallInfo = callInfo;
            CallInvoker = callInvoker;
            CommandId = commandId;
            StateMethod = stateMethod;

            ErrorConverter = errorConverter;
        }

        public override async Task Run()
        {
            try
            {
                using( var streamingCall = CallInvoker.AsyncServerStreamingCall( StateMethod, null, CreateCallOptions(), CommandId ) )
                {
                    var responseStream = streamingCall.ResponseStream;
                    while( await responseStream.MoveNext(default) )
                    {
                        var state = ConvertState( responseStream.Current.Status );
                        var estimated = responseStream.Current.EstimatedRemainingTime.Extract( null );
                        var progress = responseStream.Current.Progress.Value;
                        PushStateUpdate( progress, estimated, state );
                    }
                }
                CallInfo.FinishSuccessful();
            }
            catch (Exception exception)
            {
                var ex = ErrorHandling.ConvertException(exception, ErrorConverter);
                CallInfo.FinishWithErrors(exception);
                throw ex;
            }
        }

        internal static CommandState ConvertState(ExecutionState.CommandStatus state)
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
    }

    internal class ObservableClientCommand<TResponse, TResult> : ObservableCommand<TResult>, IClientCommand
        where TResponse : class
    {
        public IClientCallInfo CallInfo { get; }

        public CallInvoker CallInvoker { get; }

        public CommandExecutionUuid CommandId { get; }

        public Method<CommandExecutionUuid, ExecutionState> StateMethod { get; }

        public Func<DefinedExecutionErrorDto, Exception> ErrorConverter { get; }

        internal Method<CommandExecutionUuid, TResponse> ResultMethod { get; }
        internal Func<TResponse, TResult> ResultConverter { get; }

        protected override bool SendLastUpdate => false;

        public CallOptions CreateCallOptions() => CallInfo.ToCallOptions().WithCancellationToken( CancellationToken );

        /// <summary>
        /// Create a new observable command
        /// </summary>
        /// <param name="callInfo">The call info</param>
        /// <param name="callInvoker">An invoker for gRPC</param>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="stateMethod">The state method</param>
        /// <param name="errorConverter">An error converter</param>
        /// <param name="resultMethod">The method to obtain the result</param>
        /// <param name="resultConverter">A method to extract the result from the data transfer</param>
        public ObservableClientCommand(
            IClientCallInfo callInfo,
            CallInvoker callInvoker,
            CommandExecutionUuid commandId,
            Method<CommandExecutionUuid, ExecutionState> stateMethod,
            Method<CommandExecutionUuid, TResponse> resultMethod,
            Func<TResponse, TResult> resultConverter,
            Func<DefinedExecutionErrorDto, Exception> errorConverter)
            : base(new CancellationTokenSource())
        {
            CallInfo = callInfo;
            CallInvoker = callInvoker;
            CommandId = commandId;
            StateMethod = stateMethod;
            ErrorConverter = errorConverter;
            ResultMethod = resultMethod;
            ResultConverter = resultConverter;
        }

        public override async Task<TResult> Run()
        {
            try
            {
                using(var streamingCall = CallInvoker.AsyncServerStreamingCall( StateMethod, null, CreateCallOptions(), CommandId ))
                {
                    var responseStream = streamingCall.ResponseStream;
                    while(await responseStream.MoveNext(default))
                    {
                        var state = ObservableClientCommand.ConvertState( responseStream.Current.Status );
                        var estimated = responseStream.Current.EstimatedRemainingTime.Extract( null );
                        var progress = responseStream.Current.Progress.Value;
                        PushStateUpdate( progress, estimated, state );
                    }
                }

                var resultCall = CallInvoker.AsyncUnaryCall( ResultMethod, null, new CallOptions(), CommandId );
                var result = ResultConverter( await resultCall.ResponseAsync );
                CallInfo.FinishSuccessful();
                return result;
            }
            catch(Exception exception)
            {
                var ex = ErrorHandling.ConvertException( exception, ErrorConverter );
                CallInfo.FinishWithErrors( exception );
                throw ex;
            }
        }

        public new Task<TResult> Response => base.Response as Task<TResult>;
    }
}

using System;
using Grpc.Core;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Denotes helper methods to create observable command implementations that delegate their functionality via SiLA2
    /// </summary>
    public static class ObservableClientCommands
    {
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
            Func<DefinedExecutionErrorDto, Exception> errorConverter )
            where TRequest : class, ISilaRequestObject
        {
            var confirmation = callInvoker.BlockingUnaryCall( method, null,
                callInfo.ToCallOptions(), request );
            var command = new ObservableClientCommand( callInfo, callInvoker, confirmation.CommandId, stateMethod, errorConverter );
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
            Func<DefinedExecutionErrorDto, Exception> errorConverter )
            where TRequest : class, ISilaRequestObject
            where TResponse : class
        {
            var confirmation = callInvoker.BlockingUnaryCall( method, null,
                callInfo.ToCallOptions(), request );
            var command = new ObservableClientCommand<TResponse, TResult>( callInfo, callInvoker, confirmation.CommandId, stateMethod, resultMethod, resultConverter,
                errorConverter );
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
            Func<DefinedExecutionErrorDto, Exception> errorConverter )
            where TRequest : class
            where TIntermediateResponse : class
        {
            var confirmation = callInvoker.BlockingUnaryCall( method, null,
                callInfo.ToCallOptions(), request );
            var command = new ObservableIntermediateClientCommand<TIntermediateResponse, TIntermediate>( callInfo, callInvoker, confirmation.CommandId, stateMethod, intermediateMethod, intermediateConverter, errorConverter );
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
            Func<DefinedExecutionErrorDto, Exception> errorConverter )
            where TRequest : class
            where TIntermediateResponse : class
            where TResponse : class
        {
            var confirmation = callInvoker.BlockingUnaryCall( method, null,
                callInfo.ToCallOptions(), request );
            var command = new ObservableIntermediateClientCommand<TIntermediateResponse, TIntermediate, TResponse, TResult>( callInfo, callInvoker, confirmation.CommandId, stateMethod, intermediateMethod, resultMethod, intermediateConverter, resultConverter, errorConverter );
            command.Start();
            return command;
        }
    }
}
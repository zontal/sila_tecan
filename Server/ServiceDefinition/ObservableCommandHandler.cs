using Common.Logging;
using Grpc.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class ObservableCommandHandler<TRequest> : IRequestHandler, ICommandRequestHandler where TRequest : ISilaRequestObject
    {
        protected readonly SiLAServer _server;
        private static ILog _loggingChannel = LogManager.GetLogger<ObservableCommandHandler<TRequest>>();
        private readonly Func<TRequest, IObservableCommand> _implementation;
        protected readonly Func<Exception, Exception> _errorConverter;
        private readonly IIntermediateRequestHandler _intermediateHandler;

        private readonly Marshaller<TRequest> _requestMarshaller;

        public ObservableCommandHandler( SiLAServer server, Func<TRequest, IObservableCommand> implementation, Func<Exception, Exception> errorConverter, IIntermediateRequestHandler intermediateHandler, Marshaller<TRequest> requestMarshaller )
        {
            _server = server;
            _implementation = implementation;
            _errorConverter = errorConverter ?? server.ErrorHandling.CreateUndefinedExecutionError;
            _intermediateHandler = intermediateHandler;
            _requestMarshaller = requestMarshaller;
        }

        public Task QueryState( CommandExecutionUuid commandId, IServerStreamWriter<ExecutionState> responseStream, ServerCallContext context )
        {
            return QueryState( _server.GetCommandExecution( commandId.CommandId ), responseStream, context.CancellationToken );
        }

        private async Task QueryState( ObservableCommandExecution command, IAsyncStreamWriter<ExecutionState> responseStream, CancellationToken cancellationToken )
        {
            if(command.Command.State.State == CommandState.FinishedSuccess)
            {
                await responseStream.WriteAsync( ConvertStateUpdate( command.Command.State ) );
                return;
            }
            var stateReader = command.Command.StateUpdates;
            try
            {
                while(await stateReader.WaitToReadAsync( cancellationToken ))
                {
                    if(stateReader.TryRead( out var currentState ))
                    {
                        await responseStream.WriteAsync( ConvertStateUpdate( currentState ) );
                    }
                }
            }
            catch(OperationCanceledException exception)
            {
                throw _server.ErrorHandling.CreateCancellationError( exception.Message );
            }
            catch(Exception exception)
            {
                var convertedException = _errorConverter.Invoke( exception );
                if(convertedException != null && convertedException != exception)
                {
                    throw convertedException;
                }
                else
                {
                    throw;
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

        public Task<EmptyRequest> GetResponse( CommandExecutionUuid commandId, ServerCallContext context )
        {
            return GetResponse( _server.GetCommandExecution( commandId.CommandId ) );
        }

        private async Task<EmptyRequest> GetResponse( ObservableCommandExecution command )
        {
            if(command.Command.State.State != CommandState.FinishedSuccess && command.Command.State.State != CommandState.FinishedWithErrors)
            {
                throw ServerErrorHandling.CreateFrameworkError( FrameworkErrorType.CommandExecutionNotFinished, "The command has not finished execution" );
            }
            try
            {
                await command.Command.Response;
                return EmptyRequest.Instance;
            }
            catch(OperationCanceledException exception)
            {
                throw _server.ErrorHandling.CreateCancellationError( exception.Message );
            }
            catch(CommandExecutionRejectedException)
            {
                throw ServerErrorHandling.CreateFrameworkError( FrameworkErrorType.CommandExecutionNotAccepted );
            }
            catch(Exception otherException)
            {
                var convertedException = _errorConverter.Invoke( otherException );
                if(convertedException != null && convertedException != otherException)
                {
                    throw convertedException;
                }
                else
                {
                    throw;
                }
            }
        }

        public Task<CommandConfirmation> Execute( TRequest request, ServerCallContext context )
        {
            try
            {
                var executionId = _server.AddObservableCommandExecution( request.CommandIdentifier, _implementation?.Invoke( request ), new MetadataRepository( context ) );
                return Task.FromResult( new CommandConfirmation( executionId ) );
            }
            catch(CommandExecutionRejectedException)
            {
                throw ServerErrorHandling.CreateFrameworkError( FrameworkErrorType.CommandExecutionNotAccepted );
            }
            catch(Exception ex)
            {
                var convertedException = _errorConverter.Invoke( ex );
                if(convertedException != null && convertedException != ex)
                {
                    return Task.FromException<CommandConfirmation>( convertedException );
                }
                else
                {
                    return Task.FromException<CommandConfirmation>( ex );
                }
            }
        }

        public async void HandleRequest( SilaClientMessage message, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher )
        {
            if(message.Type != SilaClientMessageType.CommandInitiation)
            {
                throw new ArgumentException( $"The message type {message.Type} is not supported.", nameof( message ) );
            }

            var parameters = message.CommandInitiation.CommandParameter;
            var request = _requestMarshaller.Deserializer( parameters.Parameters );
            try
            {
                var executionId = _server.AddObservableCommandExecution( request.CommandIdentifier, _implementation?.Invoke( request ), parameters );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = message.RequestUuid,
                    ObservableCommandConfirmation = new ObservableCommandConfirmation
                    {
                        CommandConfirmation = new CommandConfirmation( executionId )
                    }
                } );
            }
            catch(RpcException rpc)
            {
                var error = ProtobufMarshaller<SiLAErrorDto>.FromByteArray( Convert.FromBase64String( rpc.Status.Detail ) );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = message.RequestUuid,
                    CommandError = error
                } );
            }
        }

        public async void HandleExecutionStateSubscription( string uuid, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher )
        {
            try
            {
                var cancellationToken = dispatcher.CreateCancellationToken( uuid );
                var executionId = new CommandExecutionUuid( command.CommandExecutionID );
                await QueryState( command, new ExecutionStateResponseStream( responseWriter, uuid, executionId ), cancellationToken );
            }
            catch(RpcException rpc)
            {
                var error = ProtobufMarshaller<SiLAErrorDto>.FromByteArray( Convert.FromBase64String( rpc.Status.Detail ) );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = uuid,
                    CommandError = error
                } );
            }
            catch(Exception exception)
            {
                if(command.Command.Response.IsCanceled)
                {
                    _loggingChannel.Info( $"Cancelling execution subscription of command {command.CommandExecutionID} because command has been cancelled" );
                }
                else
                {
                    _loggingChannel.Error( $"Subscription to execution states of command {command.CommandExecutionID} unexpectedly cancelled", exception );
                }
            }
            finally
            {
                dispatcher.VoidCancellationToken( uuid );
            }
        }

        public void HandleIntermediatesSubscription( string uuid, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher )
        {
            if(_intermediateHandler != null)
            {
                _intermediateHandler.HandleIntermediatesSubscription( uuid, command, responseWriter, dispatcher );
            }
            else
            {
                _ = responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = uuid,
                    CommandError = new SiLAErrorDto
                    {
                        FrameworkError = new FrameworkErrorDto
                        {
                            Type = FrameworkErrorType.InvalidCommandExecutionUuid,
                            Message = "The command does not support intermediate values"
                        }
                    }
                } );
            }
        }

        public virtual async void HandleResultRequest( string uuid, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher )
        {
            try
            {
                await GetResponse( command );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = uuid,
                    ObservableCommandResponse = new ObservableCommandResponse
                    {
                        ExecutionUUID = new CommandExecutionUuid( command.CommandExecutionID ),
                        Result = EmptyRequest.Marshaller.Serializer( EmptyRequest.Instance )
                    }
                } );
            }
            catch(RpcException rpcExc)
            {
                var error = ProtobufMarshaller<SiLAErrorDto>.FromByteArray( Convert.FromBase64String( rpcExc.Status.Detail ) );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = uuid,
                    CommandError = error
                } );
            }
        }

        private class ExecutionStateResponseStream : IAsyncStreamWriter<ExecutionState>
        {
            private readonly IAsyncStreamWriter<SilaServerMessage> _inner;
            private readonly string _requestUuid;
            private readonly CommandExecutionUuid _executionUuid;

            public ExecutionStateResponseStream( IAsyncStreamWriter<SilaServerMessage> inner, string requestUuid, CommandExecutionUuid executionUuid )
            {
                _inner = inner;
                _requestUuid = requestUuid;
                _executionUuid = executionUuid;
            }

            public WriteOptions WriteOptions { get => _inner.WriteOptions; set => _inner.WriteOptions = value; }

            public Task WriteAsync( ExecutionState message )
            {
                return _inner.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = _requestUuid,
                    ObservableCommandExecutionInfo = new ObservableCommandExecutionInfo
                    {
                        ExecutionInfo = message,
                        ExecutionUUID = _executionUuid
                    }
                } );
            }
        }
    }

    internal class ObservableCommandHandler<TRequest, TResponse, TResponseDto> : ObservableCommandHandler<TRequest> where TRequest : ISilaRequestObject
    {
        private readonly Func<TResponse, TResponseDto> _responseConverter;
        private readonly Marshaller<TResponseDto> _responseMarshaller;

        public ObservableCommandHandler( 
            SiLAServer server,
            Func<TRequest, IObservableCommand> implementation,
            Func<TResponse, TResponseDto> responseConverter,
            Func<Exception, Exception> errorConverter,
            IIntermediateRequestHandler intermediateHandler,
            Marshaller<TRequest> requestMarshaller,
            Marshaller<TResponseDto> responseMarshaller )
            : base( server, implementation, errorConverter, intermediateHandler, requestMarshaller )
        {
            _responseConverter = responseConverter;
            _responseMarshaller = responseMarshaller;
        }

        public Task<TResponseDto> GetCastedResponse( CommandExecutionUuid commandId, ServerCallContext context )
        {
            return GetCastedResponse( _server.GetCommandExecution( commandId.CommandId ) );
        }

        private async Task<TResponseDto> GetCastedResponse( ObservableCommandExecution command )
        {
            if(command.Command.State.State != CommandState.FinishedSuccess && command.Command.State.State != CommandState.FinishedWithErrors)
            {
                throw ServerErrorHandling.CreateFrameworkError( FrameworkErrorType.CommandExecutionNotFinished, "The command has not finished execution" );
            }
            try
            {
                var resultTask = command.Command.Response as Task<TResponse>;
                if(resultTask == null)
                {
                    throw new ArgumentOutOfRangeException( nameof( command ) );
                }

                return _responseConverter( await resultTask );
            }
            catch(OperationCanceledException exception)
            {
                throw _server.ErrorHandling.CreateCancellationError( exception.Message );
            }
            catch(NotSupportedException)
            {
                throw ServerErrorHandling.CreateFrameworkError( FrameworkErrorType.CommandExecutionNotAccepted );
            }
            catch(Exception otherException)
            {
                var convertedException = _errorConverter.Invoke( otherException );
                if(convertedException != null && convertedException != otherException)
                {
                    throw convertedException;
                }
                else
                {
                    throw;
                }
            }
        }

        public override async void HandleResultRequest( string uuid, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher )
        {
            try
            {
                var result = await GetCastedResponse( command );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = uuid,
                    ObservableCommandResponse = new ObservableCommandResponse
                    {
                        ExecutionUUID = new CommandExecutionUuid( command.CommandExecutionID ),
                        Result = _responseMarshaller.Serializer( result )
                    }
                } );
            }
            catch(RpcException rpcExc)
            {
                var error = ProtobufMarshaller<SiLAErrorDto>.FromByteArray( Convert.FromBase64String( rpcExc.Status.Detail ) );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = uuid,
                    CommandError = error
                } );
            }
        }
    }
}

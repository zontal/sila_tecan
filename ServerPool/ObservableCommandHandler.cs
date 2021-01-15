using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.ServerPooling
{
    internal class ObservableCommandHandler : ObservableCommand, IResponseHandler
    {
        private readonly TaskCompletionSource<bool> _completionSource;
        private readonly IServerStreamWriter<SilaClientMessage> _requestStream;
        private readonly string _requestUuid;
        private CommandExecutionUuid _executionUuid;
        private readonly Func<string, string, Exception> _errorConverter;
        private readonly Action _cleanupAction;
        

        public ObservableCommandHandler(string requestUuid, IServerStreamWriter<SilaClientMessage> requestStream, Func<string, string, Exception> errorConverter, Action cleanupAction )
        {
            _completionSource = new TaskCompletionSource<bool>();
            _requestUuid = requestUuid;
            _requestStream = requestStream;
            _cleanupAction = cleanupAction;
            _errorConverter = errorConverter;
            CancellationToken.Register( _completionSource.SetCanceled );
        }

        public bool ProcessMessage( SilaServerMessage serverMessage )
        {
            switch(serverMessage.Type)
            {
                case SilaServerMessageType.ObservableCommandConfirmation:
                    _executionUuid = serverMessage.ObservableCommandConfirmation.CommandConfirmation.CommandId;
                    TraceCommandExecution( _executionUuid );
                    return true;
                case SilaServerMessageType.ObservableCommandExecutionInfo:
                    var stateUpdate = serverMessage.ObservableCommandExecutionInfo.ExecutionInfo;
                    PushStateUpdate( stateUpdate.Progress.Value, stateUpdate.EstimatedRemainingTime.Extract( null ), ConvertCommandState( stateUpdate.Status ) );
                    if(stateUpdate.Status == ExecutionState.CommandStatus.FinishedSuccessfully)
                    {
                        _ = _requestStream.WriteAsync( new SilaClientMessage
                        {
                            RequestUuid = _requestUuid,
                            CommandGetResponse = new CommandExecutionReference
                            {
                                ExecutionUUID = _executionUuid
                            }
                        } );
                    }
                    return true;
                case SilaServerMessageType.ObservableCommandIntermediateResponse:
                    return HandleIntermediateResponse( serverMessage.ObservableCommandIntermediateResponse );
                case SilaServerMessageType.ObservableCommandResponse:
                    _completionSource.TrySetResult( true );
                    return true;
                case SilaServerMessageType.CommandError:
                    _completionSource.TrySetException( ErrorHandling.ConvertException( serverMessage.CommandError?.SilaError, _errorConverter ) );
                    return true;
                default:
                    return false;
            }
        }

        protected virtual void TraceCommandExecution( CommandExecutionUuid executionUuid )
        {
            SendMessage( new SilaClientMessage
            {
                CommandExecutionInfoSubscription = new CommandExecutionReference
                {
                    ExecutionUUID = executionUuid
                }
            } );
        }

        protected void SendMessage(SilaClientMessage message)
        {
            message.RequestUuid = _requestUuid;
            _ = _requestStream.WriteAsync( message );
        }

        protected virtual bool HandleIntermediateResponse(ObservableCommandResponse intermediateResponse)
        {
            return false;
        }

        internal static CommandState ConvertCommandState( ExecutionState.CommandStatus status )
        {
            switch(status)
            {
                case ExecutionState.CommandStatus.Waiting:
                    return CommandState.NotStarted;
                case ExecutionState.CommandStatus.Running:
                    return CommandState.Running;
                case ExecutionState.CommandStatus.FinishedSuccessfully:
                    return CommandState.FinishedSuccess;
                case ExecutionState.CommandStatus.FinishedWithError:
                    return CommandState.FinishedWithErrors;
                default:
                    return CommandState.Unknown;
            }
        }

        public override void Cancel()
        {
            base.Cancel();
            if (_executionUuid != null)
            {
                _requestStream.WriteAsync( new SilaClientMessage
                {
                    RequestUuid = _requestUuid,
                    CancelCommandExecutionInfoSubscription = new EmptyMessage()
                } );
            }
        }

        public override Task Run()
        {
            return _completionSource.Task.ContinueWith( t => { _cleanupAction(); } );
        }
    }

    internal class ObservableCommandHandler<TResponse> : ObservableCommand<TResponse>, IResponseHandler
    {
        private readonly TaskCompletionSource<TResponse> _completionSource;
        private readonly IServerStreamWriter<SilaClientMessage> _requestStream;
        private readonly string _requestUuid;
        private CommandExecutionUuid _executionUuid;
        private readonly Func<string, string, Exception> _errorConverter;
        private readonly Func<byte[], TResponse> _responseSerializer;
        private readonly Action _cleanupAction;

        public ObservableCommandHandler( string requestUuid, IServerStreamWriter<SilaClientMessage> requestStream, Func<string, string, Exception> errorConverter, Func<byte[], TResponse> responseSerializer, Action cleanupAction )
        {
            _completionSource = new TaskCompletionSource<TResponse>();
            _requestUuid = requestUuid;
            _requestStream = requestStream;
            _responseSerializer = responseSerializer;
            _errorConverter = errorConverter;
            _cleanupAction = cleanupAction;
            CancellationToken.Register( _completionSource.SetCanceled );
        }

        public bool ProcessMessage( SilaServerMessage serverMessage )
        {
            switch(serverMessage.Type)
            {
                case SilaServerMessageType.ObservableCommandConfirmation:
                    _executionUuid = serverMessage.ObservableCommandConfirmation.CommandConfirmation.CommandId;
                    TraceCommandExecution( _executionUuid );
                    return true;
                case SilaServerMessageType.ObservableCommandExecutionInfo:
                    var stateUpdate = serverMessage.ObservableCommandExecutionInfo.ExecutionInfo;
                    PushStateUpdate( stateUpdate.Progress.Value, stateUpdate.EstimatedRemainingTime.Extract( null ), ObservableCommandHandler.ConvertCommandState( stateUpdate.Status ) );
                    if(stateUpdate.Status == ExecutionState.CommandStatus.FinishedSuccessfully)
                    {
                        _ = _requestStream.WriteAsync( new SilaClientMessage
                        {
                            RequestUuid = _requestUuid,
                            CommandGetResponse = new CommandExecutionReference
                            {
                                ExecutionUUID = _executionUuid
                            }
                        } );
                    }
                    return true;
                case SilaServerMessageType.ObservableCommandIntermediateResponse:
                    return HandleIntermediateResponse( serverMessage.ObservableCommandIntermediateResponse );
                case SilaServerMessageType.ObservableCommandResponse:
                    _completionSource.TrySetResult( _responseSerializer( serverMessage.ObservableCommandResponse.Result ) );
                    return true;
                case SilaServerMessageType.CommandError:
                    _completionSource.TrySetException( ErrorHandling.ConvertException(serverMessage.CommandError.SilaError, _errorConverter) );
                    return true;
                default:
                    return false;
            }
        }

        protected virtual void TraceCommandExecution( CommandExecutionUuid executionUuid )
        {
            SendMessage( new SilaClientMessage
            {
                CommandExecutionInfoSubscription = new CommandExecutionReference
                {
                    ExecutionUUID = executionUuid
                }
            } );
        }

        protected void SendMessage( SilaClientMessage message )
        {
            message.RequestUuid = _requestUuid;
            _ = _requestStream.WriteAsync( message );
        }

        protected virtual bool HandleIntermediateResponse( ObservableCommandResponse intermediateResponse )
        {
            return false;
        }

        public override void Cancel()
        {
            base.Cancel();
            if(_executionUuid != null)
            {
                _requestStream.WriteAsync( new SilaClientMessage
                {
                    RequestUuid = _requestUuid,
                    CancelCommandExecutionInfoSubscription = new EmptyMessage()
                } );
            }
        }

        public override Task<TResponse> Run()
        {
            return _completionSource.Task.ContinueWith( t =>
             {
                 _cleanupAction();
                 return t.Result;
             } );
        }
    }
}

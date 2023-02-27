using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.ServerPooling
{
    internal class ObservableCommandHandler : ObservableCommand, IResponseHandler, IClientCommand
    {
        private readonly TaskCompletionSource<bool> _completionSource;
        private readonly IServerStreamWriter<SilaClientMessage> _requestStream;
        private readonly string _requestUuid;
        private readonly TaskCompletionSource<CommandExecutionUuid> _executionUuid = new TaskCompletionSource<CommandExecutionUuid>();
        private readonly Func<string, string, Exception> _errorConverter;
        private readonly Action _cleanupAction;

        public CommandExecutionUuid CommandId => _executionUuid.Task.Result;

        public ObservableCommandHandler(string requestUuid, IServerStreamWriter<SilaClientMessage> requestStream, Func<string, string, Exception> errorConverter, Action cleanupAction )
            : base( new CancellationTokenSource() )
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
                    var executionUuid = serverMessage.ObservableCommandConfirmation.CommandConfirmation.CommandId;
                    TraceCommandExecution( executionUuid );
                    _executionUuid.SetResult( executionUuid );
                    return true;
                case SilaServerMessageType.ObservableCommandExecutionInfo:
                    var stateUpdate = serverMessage.ObservableCommandExecutionInfo.ExecutionInfo;
                    PushStateUpdate( stateUpdate.Progress.Value, stateUpdate.EstimatedRemainingTime.Extract( null ), ConvertCommandState( stateUpdate.Status ) );
                    if(stateUpdate.Status == ExecutionState.CommandStatus.FinishedSuccessfully && _executionUuid.Task.IsCompleted)
                    {
                        _ = _requestStream.WriteAsync( new SilaClientMessage
                        {
                            RequestUuid = _requestUuid,
                            CommandGetResponse = new CommandExecutionReference
                            {
                                ExecutionUUID = _executionUuid.Task.Result
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
                    _completionSource.TrySetException( ErrorHandling.ConvertException( serverMessage.CommandError, _errorConverter ) );
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
                case ExecutionState.CommandStatus.FinishedSuccessfully:
                    return CommandState.FinishedSuccess;
                case ExecutionState.CommandStatus.FinishedWithError:
                    return CommandState.FinishedWithErrors;
                case ExecutionState.CommandStatus.Running:
                default:
                    return CommandState.Running;
            }
        }

        public override void Cancel()
        {
            base.Cancel();
            if (_executionUuid.Task.IsCompleted)
            {
                _requestStream.WriteAsync( new SilaClientMessage
                {
                    RequestUuid = _requestUuid,
                    CancelCommandExecutionInfoSubscription = new EmptyMessage()
                } );
            }
        }

        public override async Task Run()
        {
            try
            {
                await _completionSource.Task;
            }
            catch(TaskCanceledException)
            {
                throw new OperationCanceledException();
            }
        }

        protected override void Cleanup( Exception exception )
        {
            base.Cleanup( exception );
            _cleanupAction();
        }

        protected override bool SendLastUpdate => false;
    }

    internal class ObservableCommandHandler<TResponse> : ObservableCommand<TResponse>, IResponseHandler, IClientCommand
    {
        private readonly TaskCompletionSource<TResponse> _completionSource;
        private readonly IServerStreamWriter<SilaClientMessage> _requestStream;
        private readonly string _requestUuid;
        private readonly TaskCompletionSource<CommandExecutionUuid> _executionUuid = new TaskCompletionSource<CommandExecutionUuid>();
        private readonly Func<string, string, Exception> _errorConverter;
        private readonly Func<byte[], TResponse> _responseSerializer;
        private readonly Action _cleanupAction;

        public CommandExecutionUuid CommandId => _executionUuid.Task.Result;

        public ObservableCommandHandler( string requestUuid, IServerStreamWriter<SilaClientMessage> requestStream, Func<string, string, Exception> errorConverter, Func<byte[], TResponse> responseSerializer, Action cleanupAction )
            : base(new CancellationTokenSource())
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
                    var executionUuid = serverMessage.ObservableCommandConfirmation.CommandConfirmation.CommandId;
                    TraceCommandExecution( executionUuid );
                    _executionUuid.SetResult( executionUuid );
                    return true;
                case SilaServerMessageType.ObservableCommandExecutionInfo:
                    var stateUpdate = serverMessage.ObservableCommandExecutionInfo.ExecutionInfo;
                    PushStateUpdate( stateUpdate.Progress.Value, stateUpdate.EstimatedRemainingTime.Extract( null ), ObservableCommandHandler.ConvertCommandState( stateUpdate.Status ) );
                    if(stateUpdate.Status == ExecutionState.CommandStatus.FinishedSuccessfully && _executionUuid.Task.IsCompleted)
                    {
                        _ = _requestStream.WriteAsync( new SilaClientMessage
                        {
                            RequestUuid = _requestUuid,
                            CommandGetResponse = new CommandExecutionReference
                            {
                                ExecutionUUID = _executionUuid.Task.Result
                            }
                        } );
                    }
                    return true;
                case SilaServerMessageType.ObservableCommandIntermediateResponse:
                    return HandleIntermediateResponse( serverMessage.ObservableCommandIntermediateResponse );
                case SilaServerMessageType.ObservableCommandResponse:
                    var result = _responseSerializer( serverMessage.ObservableCommandResponse.Result );
                    _completionSource.SetResult( result );
                    return true;
                case SilaServerMessageType.CommandError:
                    _completionSource.SetException( ErrorHandling.ConvertException(serverMessage.CommandError, _errorConverter) );
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
            if(_executionUuid.Task.IsCompleted)
            {
                _requestStream.WriteAsync( new SilaClientMessage
                {
                    RequestUuid = _requestUuid,
                    CancelCommandExecutionInfoSubscription = new EmptyMessage()
                } );
            }
        }

        public override async Task<TResponse> Run()
        {
            try
            {
                return await _completionSource.Task;
            }
            catch(TaskCanceledException)
            {
                throw new OperationCanceledException();
            }
        }

        protected override void Cleanup( Exception exception )
        {
            base.Cleanup( exception );
            _cleanupAction();
        }

        protected override bool SendLastUpdate => false;
    }
}

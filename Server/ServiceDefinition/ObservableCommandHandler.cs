using Grpc.Core;
using System;
using System.Threading.Tasks;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class ObservableCommandHandler<TRequest> where TRequest : ISilaRequestObject
    {
        protected readonly SiLAServer _server;
        private readonly Func<TRequest, IObservableCommand> _implementation;
        protected readonly Func<Exception, Exception> _errorConverter;

        public ObservableCommandHandler( SiLAServer server, Func<TRequest, IObservableCommand> implementation, Func<Exception, Exception> errorConverter )
        {
            _server = server;
            _implementation = implementation;
            _errorConverter = errorConverter;
        }

        public async Task QueryState( CommandExecutionUuid commandId, IServerStreamWriter<ExecutionState> responseStream, ServerCallContext context )
        {
            var command = _server.GetCommandExecution( commandId.CommandId );
            var stateReader = command.StateUpdates;
            try
            {
                while(await stateReader.WaitToReadAsync( context.CancellationToken ))
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
                var convertedException = _errorConverter?.Invoke( exception );
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

        public async Task<EmptyRequest> GetResponse( CommandExecutionUuid commandId, ServerCallContext context )
        {
            try
            {
                var command = _server.GetCommandExecution( commandId.CommandId );
                await command.Response;
                return EmptyRequest.Instance;
            }
            catch(OperationCanceledException exception)
            {
                throw _server.ErrorHandling.CreateCancellationError( exception.Message );
            }
            catch(Exception otherException)
            {
                var convertedException = _errorConverter?.Invoke( otherException );
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
            var executionId = _server.AddObservableCommandExecution( request.CommandIdentifier, _implementation?.Invoke(request), context );
            return Task.FromResult( new CommandConfirmation( executionId ) );
        }

    }

    internal class ObservableCommandHandler<TRequest, TResponse, TResponseDto> : ObservableCommandHandler<TRequest> where TRequest : ISilaRequestObject
    {
        private readonly Func<TResponse, TResponseDto> _responseConverter;

        public ObservableCommandHandler( SiLAServer server, Func<TRequest, IObservableCommand> implementation, Func<TResponse, TResponseDto> responseConverter, Func<Exception, Exception> errorConverter )
            : base(server, implementation, errorConverter)
        {
            _responseConverter = responseConverter;
        }

        public async Task<TResponseDto> GetCastedResponse( CommandExecutionUuid commandId, ServerCallContext context )
        {

            try
            {
                var command = _server.GetCommandExecution( commandId.CommandId );
                var resultTask = command.Response as Task<TResponse>;
                if(resultTask == null)
                {
                    throw new ArgumentOutOfRangeException( nameof( commandId ) );
                }

                return _responseConverter( await resultTask );
            }
            catch(OperationCanceledException exception)
            {
                throw _server.ErrorHandling.CreateCancellationError( exception.Message );
            }
            catch(Exception otherException)
            {
                var convertedException = _errorConverter?.Invoke( otherException );
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

    }
}

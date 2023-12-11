using Grpc.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class IntermediateCommandHandler<TIntermediate, TIntermediateDto> : IIntermediateRequestHandler
    {
        private readonly SiLAServer _server;
        private readonly Func<TIntermediate, TIntermediateDto> _intermediateConverter;
        private readonly Marshaller<TIntermediateDto> _marshaller;
        private readonly Func<Exception, Exception> _errorConverter;

        public IntermediateCommandHandler( SiLAServer server, Func<TIntermediate, TIntermediateDto> intermediateConverter, Func<Exception, Exception> errorConverter, Marshaller<TIntermediateDto> marshaller )
        {
            _server = server;
            _errorConverter = errorConverter;
            _intermediateConverter = intermediateConverter;
            _marshaller = marshaller;
        }

        public Task GetIntermediateResponses( CommandExecutionUuid commandId, IServerStreamWriter<TIntermediateDto> responseStream, ServerCallContext context )
        {
            return GetIntermediateResponses( _server.GetCommandExecution( commandId.CommandId ), responseStream, context.CancellationToken );
        }

        private async Task GetIntermediateResponses( ObservableCommandExecution command, IAsyncStreamWriter<TIntermediateDto> responseStream, CancellationToken cancellationToken )
        {
            try
            {
                var reader = ((IIntermediateObservableCommand<TIntermediate>)(command.Command)).IntermediateValues;

                while(await reader.WaitToReadAsync( cancellationToken ))
                {
                    if(reader.TryRead( out var intermediate ))
                    {
                        await responseStream.WriteAsync( _intermediateConverter( intermediate ) );
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
                if( convertedException != null && convertedException != exception)
                {
                    throw convertedException;
                }
                else
                {
                    throw;
                }
            }
        }

        public async void HandleIntermediatesSubscription( string requestUuid, ObservableCommandExecution command, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher )
        {
            try
            {
                var cancellationToken = dispatcher.CreateCancellationToken( requestUuid );
                var executionId = new CommandExecutionUuid( command.CommandExecutionID );
                await GetIntermediateResponses( command, new IntermediateResponseStream( responseWriter, requestUuid, executionId, _marshaller ), cancellationToken );
            }
            finally
            {
                dispatcher.VoidCancellationToken( requestUuid );
            }
        }

        private class IntermediateResponseStream : IAsyncStreamWriter<TIntermediateDto>
        {
            private readonly IClientStreamWriter<SilaServerMessage> _responseWriter;
            private readonly string _requestUuid;
            private readonly CommandExecutionUuid _executionId;
            private readonly Marshaller<TIntermediateDto> _responseMarshaller;

            public IntermediateResponseStream( IClientStreamWriter<SilaServerMessage> responseWriter, string requestUuid, CommandExecutionUuid executionId, Marshaller<TIntermediateDto> marshaller )
            {
                _responseWriter = responseWriter;
                _requestUuid = requestUuid;
                _executionId = executionId;
                _responseMarshaller = marshaller;
            }

            public WriteOptions WriteOptions { get => _responseWriter.WriteOptions; set => _responseWriter.WriteOptions = value; }

            public Task WriteAsync( TIntermediateDto message )
            {
                return _responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = _requestUuid,
                    ObservableCommandIntermediateResponse = new ObservableCommandResponse
                    {
                        ExecutionUUID = _executionId,
                        Result = _responseMarshaller.Serializer( message )
                    }
                } );
            }
        }
    }
}

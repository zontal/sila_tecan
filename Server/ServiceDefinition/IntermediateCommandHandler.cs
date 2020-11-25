using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class IntermediateCommandHandler<TIntermediate, TIntermediateDto>
    {
        private SiLAServer _server;
        private Func<TIntermediate, TIntermediateDto> _intermediateConverter;
        private Func<Exception, Exception> _errorConverter;

        public IntermediateCommandHandler( SiLAServer server, Func<TIntermediate, TIntermediateDto> intermediateConverter, Func<Exception, Exception> errorConverter )
        {
            _server = server;
            _errorConverter = errorConverter;
            _intermediateConverter = intermediateConverter;
        }

        public async Task GetIntermediateResponses( CommandExecutionUuid commandId, IServerStreamWriter<TIntermediateDto> responseStream, ServerCallContext context )
        {
            try
            {
                var command = (IIntermediateObservableCommand<TIntermediate>)_server.GetCommandExecution( commandId.CommandId );
                var reader = command.IntermediateValues;

                while(await reader.WaitToReadAsync( context.CancellationToken ))
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

    }
}

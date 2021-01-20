using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class UnobservableCommandHandler<TRequest, TResponse> : IRequestHandler where TRequest : ISilaRequestObject
    {
        private readonly SiLAServer _server;
        private readonly Func<TRequest, TResponse> _implementation;

        public UnobservableCommandHandler( SiLAServer server, Func<TRequest, TResponse> implementation )
        {
            _server = server;
            _implementation = implementation;
        }

        public Task<TResponse> Execute( TRequest request, ServerCallContext context )
        {
            return _server.InvokeCommand( _implementation, request, new MetadataRepository(context) );
        }

        public async void HandleRequest( SilaClientMessage message, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher )
        {
            if(message.Type != SilaClientMessageType.CommandExecution)
            {
                throw new ArgumentException( $"The message type {message.Type} is not supported.", nameof( message ) );
            }

            var parameters = message.CommandExecution.CommandParameter;
            var request = ProtobufMarshaller<TRequest>.Default.Deserializer( parameters.Parameters );
            try
            {
                var response = await _server.InvokeCommand( _implementation, request, parameters );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = message.RequestUuid,
                    CommandResponse = new CommandResponse
                    {
                        Result = ProtobufMarshaller<TResponse>.ToByteArray( response )
                    }
                } );
            }
            catch(RpcException rpc)
            {
                var error = ProtobufMarshaller<SiLAErrorDto>.FromByteArray( Convert.FromBase64String( rpc.Status.Detail ) );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = message.RequestUuid,
                    CommandError = new CommandError
                    {
                        SilaError = error
                    }
                } );
            }
        }
    }
}

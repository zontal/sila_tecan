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

        private readonly Marshaller<TRequest> _requestMarshaller;
        private readonly Marshaller<TResponse> _responseMarshaller;

        public UnobservableCommandHandler(SiLAServer server, Func<TRequest, TResponse> implementation, Marshaller<TRequest> requestMarshaller, Marshaller<TResponse> responseMarshaller )
        {
            _server = server;
            _implementation = implementation;

            _requestMarshaller = requestMarshaller;
            _responseMarshaller = responseMarshaller;
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
            var request = _requestMarshaller.Deserializer( parameters.Parameters );
            try
            {
                var response = await _server.InvokeCommand( _implementation, request, parameters );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = message.RequestUuid,
                    CommandResponse = new CommandResponse
                    {
                        Result = _responseMarshaller.Serializer( response )
                    }
                } );
            }
            catch(RpcException rpc)
            {
                var error = ByteSerializer.FromByteArray<SiLAErrorDto>( Convert.FromBase64String( rpc.Status.Detail ), FrameworkTypeModel.Instance );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = message.RequestUuid,
                    CommandError = error
                } );
            }
        }
    }
}

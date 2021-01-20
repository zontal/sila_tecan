using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class UnobservablePropertyHandler<T> : IRequestHandler
    {
        private readonly SiLAServer _server;
        private readonly string _identifier;
        private readonly Func<T> _implementation;

        public UnobservablePropertyHandler( SiLAServer server, string identifier, Func<T> implementation )
        {
            _server = server;
            _identifier = identifier;
            _implementation = implementation;
        }

        public Task<T> Execute( EmptyRequest request, ServerCallContext context )
        {
            return Task.Factory.StartNew( () => _server.GetPropertyValue( _identifier, _implementation, new MetadataRepository(context) ) );
        }

        public async void HandleRequest( SilaClientMessage message, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher )
        {
            if (message.Type != SilaClientMessageType.PropertyRead)
            {
                throw new ArgumentException( $"The message type {message.Type} is not supported.", nameof( message ) );
            }

            var parameters = message.PropertyRead;
            try
            {
                var response = _server.GetPropertyValue( parameters.FullyQualifiedPropertyId, _implementation, parameters );
                await responseWriter.WriteAsync( new SilaServerMessage
                    {
                        RequestUuid = message.RequestUuid,
                        PropertyValue = new PropertyValue
                        {
                            Result = ProtobufMarshaller<T>.ToByteArray( response )
                        }
                    } );
            }
            catch(RpcException rpc)
            {
                var error = ProtobufMarshaller<SiLAErrorDto>.FromByteArray( Convert.FromBase64String( rpc.Status.Detail ) );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = message.RequestUuid,
                    PropertyError = new PropertyError
                    {
                        SilaError = error
                    }
                } );
            }
        }
    }
}

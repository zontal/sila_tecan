using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal abstract class ObservablePropertyHandlerBase<T> : IRequestHandler
    {

        private readonly SiLAServer _server;
        private readonly string _identifier;

        protected ObservablePropertyHandlerBase( SiLAServer server, string identifier )
        {
            _server = server;
            _identifier = identifier;
        }

        public Task Subscribe( EmptyRequest request, IServerStreamWriter<T> responseStream, ServerCallContext context )
        {
            var metadata = new MetadataRepository( context );
            return _server.InvokeCommand(CreateSubscriptionHandler(responseStream, context.CancellationToken), _identifier, metadata );
        }

        protected abstract Func<Task<int>> CreateSubscriptionHandler(IAsyncStreamWriter<T> responseStream, CancellationToken cancellationToken );


        public async void HandleRequest( SilaClientMessage message, IClientStreamWriter<SilaServerMessage> responseWriter, ServerPoolDispatcher dispatcher )
        {
            if(message.Type != SilaClientMessageType.PropertySubscription)
            {
                throw new ArgumentException( $"The message type {message.Type} is not supported.", nameof( message ) );
            }

            var parameters = message.PropertySubscription;
            try
            {
                var wrappedStream = new BackwardStreamWriter( responseWriter, message.RequestUuid );
                var cancellationToken = dispatcher.CreateCancellationToken( message.RequestUuid );
                await _server.InvokeCommand(CreateSubscriptionHandler(wrappedStream, cancellationToken), _identifier, parameters);
            }
            catch(RpcException rpc)
            {
                var error = ProtobufMarshaller<SiLAErrorDto>.FromByteArray( Convert.FromBase64String( rpc.Status.Detail ) );
                await responseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = message.RequestUuid,
                    PropertyError = error
                } );
            }
            finally
            {

            }
        }

        private class BackwardStreamWriter : IAsyncStreamWriter<T>
        {
            private readonly IClientStreamWriter<SilaServerMessage> _baseWriter;
            private readonly string _uuid;

            public BackwardStreamWriter(IClientStreamWriter<SilaServerMessage> baseWriter, string uuid)
            {
                _baseWriter = baseWriter;
                _uuid = uuid;
            }

            public WriteOptions WriteOptions { get => _baseWriter.WriteOptions; set => _baseWriter.WriteOptions = value; }

            public Task WriteAsync( T message )
            {
                return _baseWriter.WriteAsync( new SilaServerMessage
                {
                    RequestUuid = _uuid,
                    ObservablePropertyValue = new PropertyValue
                    {
                        Result = ProtobufMarshaller<T>.ToByteArray( message )
                    }
                } );
            }
        }
    }
}

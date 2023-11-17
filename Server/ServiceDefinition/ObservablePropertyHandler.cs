using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class ObservablePropertyHandler<T> : IRequestHandler
    {

        private readonly SiLAServer _server;
        private readonly string _identifier;
        private readonly Func<T> _implementation;
        private readonly INotifyPropertyChanged _changeSource;
        private readonly string _clientPropertyName;

        public ObservablePropertyHandler( SiLAServer server, string identifier, Func<T> implementation, INotifyPropertyChanged changeSource, string clientPropertyName )
        {
            _server = server;
            _identifier = identifier;
            _implementation = implementation;
            _changeSource = changeSource;
            _clientPropertyName = clientPropertyName;
        }

        public Task Subscribe( EmptyRequest request, IServerStreamWriter<T> responseStream, ServerCallContext context )
        {
            var metadata = new MetadataRepository( context );
            if( _changeSource != null )
            {
                return _server.InvokeCommand( () => SendValueAsync( responseStream, _implementation, _changeSource, _clientPropertyName, context.CancellationToken ), _identifier, metadata );
            }
            return _server.InvokeCommand( () => SendValueAsync( responseStream, _implementation, 1000, context.CancellationToken ), _identifier, metadata );
        }

        private static async Task<int> SendValueAsync( IAsyncStreamWriter<T> responseStream, Func<T> factory, int delay, CancellationToken cancellationToken )
        {
            do
            {
                await responseStream.WriteAsync( factory() );
                await Task.Delay( delay );
            } while(!cancellationToken.IsCancellationRequested);

            return 0;
        }

        private static async Task<int> SendValueAsync( IAsyncStreamWriter<T> responseStream, Func<T> factory, INotifyPropertyChanged changeSource, string clientPropertyName,
            CancellationToken cancellationToken )
        {
            var channel = Channels.CreateIntermediateChannel<T>();

            void HandlePropertyChanged( object sender, PropertyChangedEventArgs e )
            {
                if( e.PropertyName == clientPropertyName )
                {
                    channel.Writer.TryWrite(factory());
                }
            }

            var handler = new PropertyChangedEventHandler( HandlePropertyChanged );

            changeSource.PropertyChanged += handler;
            try
            {
                await responseStream.WriteAsync( factory() );
                do
                {
                    if (await channel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        if (channel.Reader.TryRead(out var update))
                        {
                            await responseStream.WriteAsync(update);
                        }
                    }
                } while( !cancellationToken.IsCancellationRequested );

                return 0;
            }
            catch (OperationCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                return 0;
            }
            finally
            {
                changeSource.PropertyChanged -= handler;
            }
        }

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
                if(_changeSource != null)
                {
                    await _server.InvokeCommand( () => SendValueAsync( wrappedStream, _implementation, _changeSource, _clientPropertyName, cancellationToken ), _identifier, parameters );
                }
                else
                {
                    await _server.InvokeCommand( () => SendValueAsync( wrappedStream, _implementation, 1000, cancellationToken ), _identifier, parameters );
                }
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

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class ObservablePropertyHandler<T>
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
            if( _changeSource != null )
            {
                return _server.InvokeCommand( () => SendValueAsync( responseStream, _implementation, _changeSource, _clientPropertyName, context ), _identifier, context );
            }
            return _server.InvokeCommand( () => SendValueAsync( responseStream, _implementation, 1000, context ), _identifier, context );
        }

        private static async Task<int> SendValueAsync( IServerStreamWriter<T> responseStream, Func<T> factory, int delay, ServerCallContext context )
        {
            do
            {
                await responseStream.WriteAsync( factory() );
                await Task.Delay( delay );
            } while(!context.CancellationToken.IsCancellationRequested);

            return 0;
        }

        private static async Task<int> SendValueAsync( IServerStreamWriter<T> responseStream, Func<T> factory, INotifyPropertyChanged changeSource, string clientPropertyName,
            ServerCallContext context )
        {
            var semaphore = new SemaphoreSlim(0, 1 );

            void HandlePropertyChanged( object sender, PropertyChangedEventArgs e )
            {
                if( e.PropertyName == clientPropertyName )
                {
                    semaphore.Release();
                }
            }

            var handler = new PropertyChangedEventHandler( HandlePropertyChanged );

            changeSource.PropertyChanged += handler;
            try
            {
                do
                {
                    await responseStream.WriteAsync( factory() );
                    await semaphore.WaitAsync( context.CancellationToken );
                } while( !context.CancellationToken.IsCancellationRequested );

                return 0;
            }
            finally
            {
                changeSource.PropertyChanged -= handler;
            }
        }
    }
}

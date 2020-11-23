using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class ObservablePropertyHandler<T>
    {

        private readonly SiLAServer _server;
        private readonly string _identifier;
        private readonly Func<T> _implementation;

        public ObservablePropertyHandler( SiLAServer server, string identifier, Func<T> implementation )
        {
            _server = server;
            _identifier = identifier;
            _implementation = implementation;
        }

        public Task Subscribe( EmptyRequest request, IServerStreamWriter<T> responseStream, ServerCallContext context )
        {
            return _server.InvokeCommand( () => SendValueAsync( responseStream, _implementation, 1000, context ), _identifier, context );
        }

        private static async Task<int> SendValueAsync<T>( IServerStreamWriter<T> responseStream, Func<T> factory, int delay, ServerCallContext context )
        {
            do
            {
                await responseStream.WriteAsync( factory() );
                await Task.Delay( delay );
            } while(!context.CancellationToken.IsCancellationRequested);
            return 0;
        }
    }
}

using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ServerPooling
{
    internal class PoolConnection : IDisposable
    {
        private readonly ServerPoolDispatcher _dispatcher;
        private readonly AsyncDuplexStreamingCall<SilaServerMessage, SilaClientMessage> _call;

        public PoolConnection( ServerPoolDispatcher dispatcher, AsyncDuplexStreamingCall<SilaServerMessage, SilaClientMessage> call )
        {
            _dispatcher = dispatcher;
            _call = call;
        }

        public async void Start()
        {
            while(await _call.ResponseStream.MoveNext())
            {
                _dispatcher.HandleClientRequest( _call.ResponseStream.Current, _call.RequestStream );
            }
            await _call.RequestStream.CompleteAsync();
        }

        public void Dispose()
        {
            _call.RequestStream.CompleteAsync().Wait();
        }
    }
}

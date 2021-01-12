using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.ServerPooling
{
    internal class PoolConnection : ClientBase<PoolConnection>
    {
        private ServerPoolDispatcher _dispatcher;

        protected PoolConnection( ServerPoolDispatcher dispatcher, ClientBaseConfiguration configuration ) : base( configuration )
        {
            _dispatcher = dispatcher;
        }

        public PoolConnection( ServerPoolDispatcher dispatcher, ChannelBase channel ) : base( channel )
        {
            _dispatcher = dispatcher;
        }

        public PoolConnection( ServerPoolDispatcher dispatcher, CallInvoker callInvoker ) : base( callInvoker )
        {
            _dispatcher = dispatcher;
        }

        public async Task Start(CallOptions options)
        {
            var call = CallInvoker.AsyncDuplexStreamingCall( PoolingConstants.ConnectSilaServerMethod, null, options );
            while(await call.ResponseStream.MoveNext())
            {
                _dispatcher.HandleClientRequest( call.ResponseStream.Current, call.RequestStream );
            }
            await call.RequestStream.CompleteAsync();
        }

        protected override PoolConnection NewInstance( ClientBaseConfiguration configuration )
        {
            return new PoolConnection( _dispatcher, configuration );
        }
    }
}

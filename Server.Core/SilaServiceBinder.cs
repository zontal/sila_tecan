using Common.Logging;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Server
{
    internal class SilaServiceBinder : ServiceBinderBase
    {
        private static readonly ILog _loggingChannel = LogManager.GetLogger<SilaServiceBinder>();
        private static readonly IList<object> _emptyMetadata = new List<object>().AsReadOnly();
        private readonly IServiceConfigurationBuilder<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>> _serviceHandlerRepository;

        public SilaServiceBinder( IServiceConfigurationBuilder<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>> serviceHandlerRepository )
        {
            _serviceHandlerRepository = serviceHandlerRepository;
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, ClientStreamingServerMethod<TRequest, TResponse> handler )
        {
            var invoker = new ClientStreamingHandler<TRequest, TResponse>( method, handler );
            _serviceHandlerRepository.AddServiceHandler( invoker );
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, DuplexStreamingServerMethod<TRequest, TResponse> handler )
        {
            var invoker = new DuplexStreamingHandler<TRequest, TResponse>( method, handler );
            _serviceHandlerRepository.AddServiceHandler( invoker );
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, ServerStreamingServerMethod<TRequest, TResponse> handler )
        {
            var invoker = new ServerStreamingHandler<TRequest, TResponse>( method, handler );
            _serviceHandlerRepository.AddServiceHandler( invoker );
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse> handler )
        {
            var invoker = new UnaryHandler<TRequest, TResponse>( method, handler );
            _serviceHandlerRepository.AddServiceHandler( invoker );
        }

        private class ClientStreamingHandler<TRequest, TResponse> : IServiceHandler<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>>
            where TRequest : class
            where TResponse : class
        {
            private readonly Method<TRequest, TResponse> _method;
            private readonly ClientStreamingServerMethod<TRequest, TResponse> _handler;

            public ClientStreamingHandler(Method<TRequest, TResponse> method, ClientStreamingServerMethod<TRequest, TResponse> handler)
            {
                _method = method;
                _handler = handler;
            }

            public async Task<TResponse> HandleCall(ISiLAServer server, IAsyncStreamReader<TRequest> requestStream, ServerCallContext context)
            {
                try
                {
                    _loggingChannel.Debug( $"{context.Method} called by {context.Peer}" );
                    return await _handler?.Invoke( requestStream, context );
                }
                catch(Exception exception)
                {
                    _loggingChannel.Error( $"{context.Method} call from {context.Peer} failed", exception );
                    throw;
                }
            }

            public void Register( ServiceMethodProviderContext<ISiLAServer> context )
            {
                context.AddClientStreamingMethod( _method, _emptyMetadata, HandleCall );
            }
        }

        private class DuplexStreamingHandler<TRequest, TResponse> : IServiceHandler<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>>
            where TRequest : class
            where TResponse : class
        {
            private readonly Method<TRequest, TResponse> _method;
            private readonly DuplexStreamingServerMethod<TRequest, TResponse> _handler;

            public DuplexStreamingHandler( Method<TRequest, TResponse> method, DuplexStreamingServerMethod<TRequest, TResponse> handler )
            {
                _method = method;
                _handler = handler;
            }

            public async Task HandleCall( ISiLAServer server, IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseWriter, ServerCallContext context )
            {
                try
                {
                    _loggingChannel.Debug( $"{context.Method} called by {context.Peer}" );
                    await _handler?.Invoke( requestStream, responseWriter, context );
                }
                catch(Exception exception)
                {
                    _loggingChannel.Error( $"{context.Method} call from {context.Peer} failed", exception );
                    throw;
                }
            }

            public void Register( ServiceMethodProviderContext<ISiLAServer> context )
            {
                context.AddDuplexStreamingMethod( _method, _emptyMetadata, HandleCall );
            }
        }

        private class ServerStreamingHandler<TRequest, TResponse> : IServiceHandler<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>>
            where TRequest : class
            where TResponse : class
        {
            private readonly Method<TRequest, TResponse> _method;
            private readonly ServerStreamingServerMethod<TRequest, TResponse> _handler;

            public ServerStreamingHandler( Method<TRequest, TResponse> method, ServerStreamingServerMethod<TRequest, TResponse> handler )
            {
                _method = method;
                _handler = handler;
            }

            public async Task HandleCall( ISiLAServer server, TRequest request, IServerStreamWriter<TResponse> responseWriter, ServerCallContext context )
            {
                try
                {
                    _loggingChannel.Debug( $"{context.Method} called by {context.Peer}" );
                    await _handler?.Invoke( request, responseWriter, context );
                }
                catch(Exception exception)
                {
                    _loggingChannel.Error( $"{context.Method} call from {context.Peer} failed", exception );
                    throw;
                }
            }

            public void Register( ServiceMethodProviderContext<ISiLAServer> context )
            {
                context.AddServerStreamingMethod( _method, _emptyMetadata, HandleCall );
            }
        }

        private class UnaryHandler<TRequest, TResponse> : IServiceHandler<ISiLAServer, ServiceMethodProviderContext<ISiLAServer>>
            where TRequest : class
            where TResponse : class
        {
            private readonly Method<TRequest, TResponse> _method;
            private readonly UnaryServerMethod<TRequest, TResponse> _handler;

            public UnaryHandler( Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse> handler )
            {
                _method = method;
                _handler = handler;
            }

            public async Task<TResponse> HandleCall( ISiLAServer server, TRequest request, ServerCallContext context )
            {
                try
                {
                    _loggingChannel.Debug( $"{context.Method} called by {context.Peer}" );
                    return await _handler?.Invoke( request, context );
                }
                catch(Exception exception)
                {
                    _loggingChannel.Error( $"{context.Method} call from {context.Peer} failed", exception );
                    throw;
                }
            }

            public void Register( ServiceMethodProviderContext<ISiLAServer> context )
            {
                context.AddUnaryMethod( _method, _emptyMetadata, HandleCall );
            }
        }
    }
}

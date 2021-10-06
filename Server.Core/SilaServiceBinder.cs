using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tecan.MAP.Contracts.Services.Logging;

namespace Tecan.Sila2.Server
{
    internal class SilaServiceBinder : ServiceBinderBase
    {
        private readonly ILoggingChannel _loggingChannel;
        private static readonly IList<object> _emptyMetadata = new List<object>().AsReadOnly();
        private readonly IServiceHandlerRepository<ISiLAServer> _serviceHandlerRepository;

        public SilaServiceBinder( IServiceHandlerRepository<ISiLAServer> serviceHandlerRepository, ILoggingChannel loggingChannel )
        {
            _loggingChannel = loggingChannel;
            _serviceHandlerRepository = serviceHandlerRepository;
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, ClientStreamingServerMethod<TRequest, TResponse> handler )
        {
            var invoker = new ClientStreamingHandler<TRequest, TResponse>( method, handler, _loggingChannel );
            _serviceHandlerRepository.AddServiceHandler( invoker );
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, DuplexStreamingServerMethod<TRequest, TResponse> handler )
        {
            var invoker = new DuplexStreamingHandler<TRequest, TResponse>( method, handler, _loggingChannel );
            _serviceHandlerRepository.AddServiceHandler( invoker );
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, ServerStreamingServerMethod<TRequest, TResponse> handler )
        {
            var invoker = new ServerStreamingHandler<TRequest, TResponse>( method, handler, _loggingChannel );
            _serviceHandlerRepository.AddServiceHandler( invoker );
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse> handler )
        {
            var invoker = new UnaryHandler<TRequest, TResponse>( method, handler, _loggingChannel );
            _serviceHandlerRepository.AddServiceHandler( invoker );
        }

        private class ClientStreamingHandler<TRequest, TResponse> : IServiceHandler<ISiLAServer>
            where TRequest : class
            where TResponse : class
        {
            private readonly Method<TRequest, TResponse> _method;
            private readonly ClientStreamingServerMethod<TRequest, TResponse> _handler;
            private readonly ILoggingChannel _loggingChannel;

            public ClientStreamingHandler(Method<TRequest, TResponse> method, ClientStreamingServerMethod<TRequest, TResponse> handler, ILoggingChannel loggingChannel)
            {
                _method = method;
                _handler = handler;
                _loggingChannel = loggingChannel;
            }

            public async Task<TResponse> HandleCall(ISiLAServer server, IAsyncStreamReader<TRequest> requestStream, ServerCallContext context)
            {
                try
                {
                    _loggingChannel.Log( $"{context.Method} called by {context.Peer}".ToString, Severity.Debug );
                    return await _handler?.Invoke( requestStream, context );
                }
                catch(Exception exception)
                {
                    _loggingChannel.LogException( $"{context.Method} call from {context.Peer} failed".ToString, exception );
                    throw;
                }
            }

            public void Register( ServiceMethodProviderContext<ISiLAServer> context )
            {
                context.AddClientStreamingMethod( _method, _emptyMetadata, HandleCall );
            }
        }

        private class DuplexStreamingHandler<TRequest, TResponse> : IServiceHandler<ISiLAServer>
            where TRequest : class
            where TResponse : class
        {
            private readonly Method<TRequest, TResponse> _method;
            private readonly DuplexStreamingServerMethod<TRequest, TResponse> _handler;
            private readonly ILoggingChannel _loggingChannel;

            public DuplexStreamingHandler( Method<TRequest, TResponse> method, DuplexStreamingServerMethod<TRequest, TResponse> handler, ILoggingChannel loggingChannel )
            {
                _method = method;
                _handler = handler;
                _loggingChannel = loggingChannel;
            }

            public async Task HandleCall( ISiLAServer server, IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseWriter, ServerCallContext context )
            {
                try
                {
                    _loggingChannel.Log( $"{context.Method} called by {context.Peer}".ToString, Severity.Debug );
                    await _handler?.Invoke( requestStream, responseWriter, context );
                }
                catch(Exception exception)
                {
                    _loggingChannel.LogException( $"{context.Method} call from {context.Peer} failed".ToString, exception );
                    throw;
                }
            }

            public void Register( ServiceMethodProviderContext<ISiLAServer> context )
            {
                context.AddDuplexStreamingMethod( _method, _emptyMetadata, HandleCall );
            }
        }

        private class ServerStreamingHandler<TRequest, TResponse> : IServiceHandler<ISiLAServer>
            where TRequest : class
            where TResponse : class
        {
            private readonly Method<TRequest, TResponse> _method;
            private readonly ServerStreamingServerMethod<TRequest, TResponse> _handler;
            private readonly ILoggingChannel _loggingChannel;

            public ServerStreamingHandler( Method<TRequest, TResponse> method, ServerStreamingServerMethod<TRequest, TResponse> handler, ILoggingChannel loggingChannel )
            {
                _method = method;
                _handler = handler;
                _loggingChannel = loggingChannel;
            }

            public async Task HandleCall( ISiLAServer server, TRequest request, IServerStreamWriter<TResponse> responseWriter, ServerCallContext context )
            {
                try
                {
                    _loggingChannel.Log( $"{context.Method} called by {context.Peer}".ToString, Severity.Debug );
                    await _handler?.Invoke( request, responseWriter, context );
                }
                catch(Exception exception)
                {
                    _loggingChannel.LogException( $"{context.Method} call from {context.Peer} failed".ToString, exception );
                    throw;
                }
            }

            public void Register( ServiceMethodProviderContext<ISiLAServer> context )
            {
                context.AddServerStreamingMethod( _method, _emptyMetadata, HandleCall );
            }
        }

        private class UnaryHandler<TRequest, TResponse> : IServiceHandler<ISiLAServer>
            where TRequest : class
            where TResponse : class
        {
            private readonly Method<TRequest, TResponse> _method;
            private readonly UnaryServerMethod<TRequest, TResponse> _handler;
            private readonly ILoggingChannel _loggingChannel;

            public UnaryHandler( Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse> handler, ILoggingChannel loggingChannel )
            {
                _method = method;
                _handler = handler;
                _loggingChannel = loggingChannel;
            }

            public async Task<TResponse> HandleCall( ISiLAServer server, TRequest request, ServerCallContext context )
            {
                try
                {
                    _loggingChannel.Log( $"{context.Method} called by {context.Peer}".ToString, Severity.Debug );
                    return await _handler?.Invoke( request, context );
                }
                catch(Exception exception)
                {
                    _loggingChannel.LogException( $"{context.Method} call from {context.Peer} failed".ToString, exception );
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

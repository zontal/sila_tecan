using System;
using System.Threading.Tasks;
using Common.Logging;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Tecan.Sila2.Server
{
    internal class LoggingInterceptor : Interceptor
    {
        private readonly ILog _loggingChannel;

        public LoggingInterceptor( ILog loggingChannel )
        {
            _loggingChannel = loggingChannel;
        }

        public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>( IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation )
        {
            try
            {
                _loggingChannel.Debug( $"{context.Method} called by {context.Peer}" );
                return base.ClientStreamingServerHandler( requestStream, context, continuation );
            }
            catch( Exception exception )
            {
                _loggingChannel.Error($"{context.Method} call from {context.Peer} failed", exception);
                throw;
            }
        }

        public override Task DuplexStreamingServerHandler<TRequest, TResponse>( IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation )
        {
            try
            {
                _loggingChannel.Debug( $"{context.Method} called by {context.Peer}" );
                return base.DuplexStreamingServerHandler( requestStream, responseStream, context, continuation );
            }
            catch(Exception exception)
            {
                _loggingChannel.Error( $"{context.Method} call from {context.Peer} failed", exception );
                throw;
            }
        }

        public override Task ServerStreamingServerHandler<TRequest, TResponse>( TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation )
        {
            try
            {
                _loggingChannel.Debug( $"{context.Method} called by {context.Peer}" );
                return base.ServerStreamingServerHandler( request, responseStream, context, continuation );
            }
            catch(Exception exception)
            {
                _loggingChannel.Error( $"{context.Method} call from {context.Peer} failed", exception );
                throw;
            }
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>( TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation )
        {
            try
            {
                _loggingChannel.Debug( $"{context.Method} called by {context.Peer}" );
                return base.UnaryServerHandler( request, context, continuation );
            }
            catch(Exception exception)
            {
                _loggingChannel.Error( $"{context.Method} call from {context.Peer} failed", exception );
                throw;
            }
        }
    }
}

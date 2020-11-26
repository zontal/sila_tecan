using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Tecan.MAP.Contracts.Services.Logging;

namespace Tecan.Sila2.Server
{
    internal class LoggingInterceptor : Interceptor
    {
        private readonly ILoggingChannel _loggingChannel;

        public LoggingInterceptor( ILoggingChannel loggingChannel )
        {
            _loggingChannel = loggingChannel;
        }

        public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>( IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation )
        {
            try
            {
                _loggingChannel.Log( $"{context.Method} called by {context.Peer}".ToString, Severity.Debug );
                return base.ClientStreamingServerHandler( requestStream, context, continuation );
            }
            catch( Exception exception )
            {
                _loggingChannel.LogException($"{context.Method} call from {context.Peer} failed".ToString, exception);
                throw;
            }
        }

        public override Task DuplexStreamingServerHandler<TRequest, TResponse>( IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation )
        {
            try
            {
                _loggingChannel.Log( $"{context.Method} called by {context.Peer}".ToString, Severity.Debug );
                return base.DuplexStreamingServerHandler( requestStream, responseStream, context, continuation );
            }
            catch(Exception exception)
            {
                _loggingChannel.LogException( $"{context.Method} call from {context.Peer} failed".ToString, exception );
                throw;
            }
        }

        public override Task ServerStreamingServerHandler<TRequest, TResponse>( TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation )
        {
            try
            {
                _loggingChannel.Log( $"{context.Method} called by {context.Peer}".ToString, Severity.Debug );
                return base.ServerStreamingServerHandler( request, responseStream, context, continuation );
            }
            catch(Exception exception)
            {
                _loggingChannel.LogException( $"{context.Method} call from {context.Peer} failed".ToString, exception );
                throw;
            }
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>( TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation )
        {
            try
            {
                _loggingChannel.Log( $"{context.Method} called by {context.Peer}".ToString, Severity.Debug );
                return base.UnaryServerHandler( request, context, continuation );
            }
            catch(Exception exception)
            {
                _loggingChannel.LogException( $"{context.Method} call from {context.Peer} failed".ToString, exception );
                throw;
            }
        }
    }
}

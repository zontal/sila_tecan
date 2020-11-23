using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class UnobservableCommandHandler<TRequest, TResponse> where TRequest : ISilaRequestObject
    {
        private readonly SiLAServer _server;
        private readonly Func<TRequest, TResponse> _implementation;

        public UnobservableCommandHandler( SiLAServer server, Func<TRequest, TResponse> implementation )
        {
            _server = server;
            _implementation = implementation;
        }

        public Task<TResponse> Execute( TRequest request, ServerCallContext context )
        {
            return _server.InvokeCommand( _implementation, request, context );
        }
    }
}

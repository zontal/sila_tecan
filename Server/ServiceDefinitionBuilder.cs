using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server
{
    internal class ServiceDefinitionBuilder : ServiceBinderBase
    {
        private readonly ServerServiceDefinition.Builder _builder;

        public ServiceDefinitionBuilder()
        {
            _builder = ServerServiceDefinition.CreateBuilder();
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, ClientStreamingServerMethod<TRequest, TResponse> handler )
        {
            _builder.AddMethod( method, handler );
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, DuplexStreamingServerMethod<TRequest, TResponse> handler )
        {
            _builder.AddMethod( method, handler );
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, ServerStreamingServerMethod<TRequest, TResponse> handler )
        {
            _builder.AddMethod( method, handler );
        }

        public override void AddMethod<TRequest, TResponse>( Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse> handler )
        {
            _builder.AddMethod( method, handler );
        }

        public ServerServiceDefinition Build()
        {
            return _builder.Build();
        }
    }
}

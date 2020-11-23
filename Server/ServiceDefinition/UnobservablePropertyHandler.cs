using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace Tecan.Sila2.Server.ServiceDefinition
{
    internal class UnobservablePropertyHandler<T>
    {
        private readonly SiLAServer _server;
        private readonly string _identifier;
        private readonly Func<T> _implementation;

        public UnobservablePropertyHandler( SiLAServer server, string identifier, Func<T> implementation )
        {
            _server = server;
            _identifier = identifier;
            _implementation = implementation;
        }

        public Task<T> Execute( EmptyRequest request, ServerCallContext context )
        {
            return _server.CreatePropertyTask( _identifier, _implementation(), context );
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server
{
    internal class ServiceHandlerRepository : IServiceHandlerRepository<ISiLAServer>
    {
        private readonly List<IServiceHandler<ISiLAServer>> _handlers = new List<IServiceHandler<ISiLAServer>>();
        public IEnumerable<IServiceHandler<ISiLAServer>> ServiceHandlers => _handlers;

        public void AddServiceHandler( IServiceHandler<ISiLAServer> serviceHandler )
        {
            _handlers.Add( serviceHandler );
        }
    }
}

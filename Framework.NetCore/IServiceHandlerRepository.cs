using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server
{
    public interface IServiceHandlerRepository<T> where T : class
    {
        void AddServiceHandler( IServiceHandler<T> serviceHandler );

        IEnumerable<IServiceHandler<T>> ServiceHandlers { get; }
    }
}

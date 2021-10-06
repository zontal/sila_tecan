using Grpc.AspNetCore.Server.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server
{
    public interface IServiceHandler<T>
        where T : class
    {
        void Register( ServiceMethodProviderContext<T> context );
    }
}

using Grpc.AspNetCore.Server.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes an interface for a gRPC service handler
    /// </summary>
    /// <typeparam name="T">The service type in the context of which the call is handled</typeparam>
    public interface IServiceHandler<T>
        where T : class
    {
        /// <summary>
        /// Registers the handler to the provided method provider context
        /// </summary>
        /// <param name="context">An object that allows to register gRPC method handlers</param>
        void Register( ServiceMethodProviderContext<T> context );
    }
}

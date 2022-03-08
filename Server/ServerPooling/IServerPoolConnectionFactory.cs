using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server.ServerPooling
{
    /// <summary>
    /// Denotes a component that can create pool connections
    /// </summary>
    public interface IServerPoolConnectionFactory
    {
        /// <summary>
        /// Connects to the given server pool
        /// </summary>
        /// <param name="target">The target that should be connected to</param>
        /// <returns>A call invoker for the pool connection</returns>
        CallInvoker CreatePoolConnection( string target );
    }
}

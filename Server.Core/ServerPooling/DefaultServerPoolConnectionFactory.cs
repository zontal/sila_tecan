using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server.ServerPooling
{
    /// <summary>
    /// Denotes the default implementation of connecting to a server pool
    /// </summary>
    public class DefaultServerPoolConnectionFactory : IServerPoolConnectionFactory
    {
        /// <inheritdoc />
        public CallInvoker CreatePoolConnection( string target )
        {
            var client = GrpcChannel.ForAddress( target.StartsWith( "http" ) ? target : "https://" + target );
            return client.CreateCallInvoker();
        }
    }
}

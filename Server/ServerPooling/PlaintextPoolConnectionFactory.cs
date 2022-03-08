using Common.Logging;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.ServerPooling;

namespace Tecan.Sila2.Server.ServerPooling
{
    /// <summary>
    /// Denotes a class that creates a plaintext connection to the server pool
    /// </summary>
    public class PlaintextPoolConnectionFactory : IServerPoolConnectionFactory
    {
        private static ILog _logger = LogManager.GetLogger( typeof( PlaintextPoolConnectionFactory ) );

        /// <inheritdoc />
        public CallInvoker CreatePoolConnection( string poolAddress )
        {
            _logger.Info( $"Connecting to server pool {poolAddress}" );
            var channel = new Channel( poolAddress, ChannelCredentials.Insecure );
            return channel.CreateCallInvoker();
        }
    }
}

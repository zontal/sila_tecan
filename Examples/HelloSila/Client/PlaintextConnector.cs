using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;

namespace Client
{
    internal class PlaintextConnector : ServerConnector
    {
        public PlaintextConnector(IClientExecutionManager executionManager, ILoggingChannelFactory loggingChannelFactory) : base(executionManager, loggingChannelFactory)
        {
        }

        public override ServerData Connect(string host, int port)
        {
            return GetServerData(new Grpc.Core.Channel($"{host}:{port}", ChannelCredentials.Insecure));
        }

        public override ServerData Connect(IPAddress host, int port)
        {
            return GetServerData(new Grpc.Core.Channel($"{host}:{port}", ChannelCredentials.Insecure));
        }
    }
}

using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Client;
using Tecan.Sila2.Discovery;

namespace Tecan.Sila2.IntegrationTests
{
    // Unfortunately, this still does not seem to work
    internal class WinHttpConnector : ServerConnector
    {
        public WinHttpConnector(IClientExecutionManager executionManager) : base(executionManager)
        {
        }

        protected override GrpcChannelOptions CreateChannelOptions(Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> certificateValidator)
        {
            return new GrpcChannelOptions
            {
                HttpHandler = new WinHttpHandler
                {
                    ServerCertificateValidationCallback = certificateValidator
                }
            };
        }
    }
}

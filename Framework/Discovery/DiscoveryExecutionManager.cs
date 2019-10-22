using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Discovery
{
    /// <summary>
    /// Denotes an execution manager for discovery purposes
    /// </summary>
    /// <remarks>By the standard, the SiLA service must not require any metadata and also does not use any binaries.</remarks>
    public class DiscoveryExecutionManager : IClientExecutionManager
    {
        /// <inheritdoc />
        public IClientCallInfo CreateCallOptions( string commandIdentifier )
        {
            return new EmptyCallInfo();
        }

        /// <inheritdoc />
        public IBinaryStore DownloadBinaryStore => null;

        /// <inheritdoc />
        public IBinaryStore CreateBinaryStore( string commandParameterIdentifier )
        {
            return null;
        }

        private class EmptyCallInfo : IClientCallInfo
        {
            public EmptyCallInfo()
            {
                Options = new CallOptions();
            }

            public CallOptions Options
            {
                get;
            }

            public void FinishSuccessful() { }

            public void FinishWithErrors( Exception exception ) { }
        }
    }
}

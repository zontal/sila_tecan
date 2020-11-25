using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Denotes an empty call info object that does not have any metadata
        /// </summary>
        public static readonly IClientCallInfo EmptyCallInfoInstance = new EmptyCallInfo();

        /// <inheritdoc />
        public IClientCallInfo CreateCallOptions( string commandIdentifier )
        {
            return EmptyCallInfoInstance;
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
            public IDictionary<string, byte[]> Metadata => null;

            public void FinishSuccessful() { }

            public void FinishWithErrors( Exception exception ) { }
        }
    }
}

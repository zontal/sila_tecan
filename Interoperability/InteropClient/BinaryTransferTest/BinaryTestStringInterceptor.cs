using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Interop.Client.BinaryTransferTest
{
    internal class BinaryTestStringInterceptor : IClientRequestInterceptor
    {
        public string MetadataIdentifier => "org.silastandard/test/BinaryTransferTest/v1/Metadata/String";

        public IClientRequestInterception Intercept(ServerData server, string interceptedCommand, IClientExecutionManager executionManager, IDictionary<string, byte[]> metadata)
        {
            metadata.Add(MetadataIdentifier, ByteSerializer.ToByteArray(new PropertyResponse<StringDto>("abc")));
            return null!;
        }
    }
}

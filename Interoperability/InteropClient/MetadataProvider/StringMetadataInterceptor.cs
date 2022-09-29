using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Interop.Client.MetadataProvider
{
    internal class StringMetadataInterceptor : IClientRequestInterceptor
    {
        public static string Metadata { get; set; }

        public string MetadataIdentifier => "org.silastandard/test/MetadataProviderTest/v1/Metadata/StringMetadata";

        public IClientRequestInterception Intercept( ServerData server, IClientExecutionManager executionManager, IDictionary<string, byte[]> metadata )
        {
            metadata.Add( MetadataIdentifier, ByteSerializer.ToByteArray( new PropertyResponse<StringDto>( Metadata ) ) );
            return null;
        }
    }
}

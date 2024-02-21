using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Client;
using Tecan.Sila2.Interop.Client.MetadataProvider.MetadataProvider;

namespace Tecan.Sila2.Interop.Client.MetadataProvider
{
    internal class TwoIntegersMetadataInterceptor : IClientRequestInterceptor
    {
        public static TwoIntegersMetadata Metadata { get; set; }

        public string MetadataIdentifier => "org.silastandard/test/MetadataProvider/v1/Metadata/TwoIntegersMetadata";

        public IClientRequestInterception? Intercept( ServerData server, string interceptedCommand, IClientExecutionManager executionManager, IDictionary<string, byte[]> metadata )
        {
            metadata.Add( MetadataIdentifier, ByteSerializer.ToByteArray( new PropertyResponse<TwoIntegersMetadataDto>( new TwoIntegersMetadataDto( Metadata, null ) ) ) );
            return null;
        }
    }
}

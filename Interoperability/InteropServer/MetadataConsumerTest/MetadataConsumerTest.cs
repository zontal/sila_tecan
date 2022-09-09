using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Tecan.Sila2.Interop.Server.MetadataProvider;

namespace Tecan.Sila2.Interop.Server.MetadataConsumerTest
{
    [Export( typeof( IMetadataConsumerTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class MetadataConsumerTest : IMetadataConsumerTest
    {
        [return: SilaIdentifier( "ReceivedStringMetadata" )]
        public string EchoStringMetadata()
        {
            return StringMetadataProvider.Value;
        }

        [return: InlineStruct]
        public UnpackMetadataResponse UnpackMetadata()
        {
            var integers = TwoIntegersMetadataProvider.Value;
            return new UnpackMetadataResponse( StringMetadataProvider.Value, integers.FirstInteger, integers.SecondInteger );
        }
    }
}

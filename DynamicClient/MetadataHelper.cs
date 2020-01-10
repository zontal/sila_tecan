using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.DynamicClient
{
    internal static class MetadataHelper
    {
        public static CallOptions TakeInMetadata( IClientCallInfo callInfo, IDictionary<string, byte[]> additionalMetadata )
        {
            var options = callInfo.Options;
            if(additionalMetadata != null)
            {
                var metadata = options.Headers;
                if(metadata == null)
                {
                    metadata = new Metadata();
                    options = options.WithHeaders( metadata );
                }

                foreach(var metadataEntry in additionalMetadata)
                {
                    metadata.Add( metadataEntry.Key, metadataEntry.Value );
                }
            }

            return options;
        }
    }
}

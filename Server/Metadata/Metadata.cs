using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using ProtoBuf;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes a helper class for handling metadata
    /// </summary>
    public static class Metadata
    {
        /// <summary>
        /// Extracts the metadata with the given identifier from the call context
        /// </summary>
        /// <typeparam name="TMetadata">The type of the metadata</typeparam>
        /// <param name="metadata">The metadata that was used with this request</param>
        /// <param name="metadataIdentifier">The global unique identifier of the metadata</param>
        /// <returns>The deserialized metadata</returns>
        public static TMetadata Extract<TMetadata>(IMetadataRepository metadata, string metadataIdentifier)
            where TMetadata : new()
        {
            if (metadata != null && metadata.TryGetMetadata(metadataIdentifier, out var metadataValue))
            {
                return ProtobufMarshaller<TMetadata>.FromByteArray( metadataValue );
            }

            return new TMetadata();
        }
    }
}

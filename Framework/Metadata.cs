using ProtoBuf;

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
        public static TMetadata Extract<TMetadata>( IMetadataRepository metadata, string metadataIdentifier )
            where TMetadata : new()
        {
            if(metadata != null && metadata.TryGetMetadata( metadataIdentifier, out var metadataValue ))
            {
                try
                {
                    var deserialized = ByteSerializer.FromByteArray<PropertyResponse<TMetadata>>( metadataValue );
                    if(deserialized == null || deserialized.Value == null)
                    {
                        throw new MissingMetadataException( metadataIdentifier );
                    }
                    if(deserialized.Value is ISilaTransferObject transferObject)
                    {
                        var error = transferObject.GetValidationErrors();
                        if(!string.IsNullOrEmpty( error ))
                        {
                            throw new MissingMetadataException( metadataIdentifier, error );
                        }
                    }
                    return deserialized.Value;
                }
                catch(ProtoException protoException)
                {
                    throw new MissingMetadataException( metadataIdentifier, protoException.Message );
                }
            }

            throw new MissingMetadataException( metadataIdentifier );
        }
    }
}

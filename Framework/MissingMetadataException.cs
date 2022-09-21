using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes that a metadata is missing
    /// </summary>
    public class MissingMetadataException : Exception
    {
        /// <summary>
        /// The fully qualified identifier of the metadata
        /// </summary>
        public string MetadataIdentifier { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="metadataIdentifier">The identifier of the metadata that is missing</param>
        public MissingMetadataException( string metadataIdentifier ) : base( $"The metadata for {metadataIdentifier} is missing" )
        {
            MetadataIdentifier = metadataIdentifier;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="metadataIdentifier">The identifier of the metadata that is missing</param>
        /// <param name="message">The error message</param>
        public MissingMetadataException( string metadataIdentifier, string message ) : base( message )
        {
            MetadataIdentifier = metadataIdentifier;
        }
    }
}

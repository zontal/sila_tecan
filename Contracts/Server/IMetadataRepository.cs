using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes a component that can access the metadata of a request
    /// </summary>
    public interface IMetadataRepository
    {
        /// <summary>
        /// Tries to get the metadata entry for the given metadata
        /// </summary>
        /// <param name="metadataIdentifier">The unique identifier of a metadata</param>
        /// <param name="metadata">The metadata value in serialized form</param>
        /// <returns>True, if the metadata could be restored, otherwise false</returns>
        bool TryGetMetadata( string metadataIdentifier, out byte[] metadata );

        /// <summary>
        /// Gets the number of metadata entries available
        /// </summary>
        int Count { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Server
{
    public interface IMetadataRepository
    {
        bool TryGetMetadata( string metadataIdentifier, out byte[] metadata );

        int Count { get; }
    }
}

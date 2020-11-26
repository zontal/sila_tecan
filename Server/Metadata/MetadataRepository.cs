using Grpc.Core;
using System;
using System.Linq;
using Tecan.Sila2.Client;

namespace Tecan.Sila2.Server
{
    internal class MetadataRepository : IMetadataRepository
    {
        private ServerCallContext _context;

        public MetadataRepository(ServerCallContext context)
        {
            _context = context;
        }

        public int Count => _context.RequestHeaders.Count( entry => entry.IsBinary && entry.Key.StartsWith( "sila" ) );

        public bool TryGetMetadata( string metadataIdentifier, out byte[] metadata )
        {
            var headerIdentifier = ClientMetadata.GetHeaderName( metadataIdentifier );
            foreach(var header in _context.RequestHeaders)
            {
                if(string.Equals( header.Key, headerIdentifier, StringComparison.OrdinalIgnoreCase ))
                {
                    metadata = header.ValueBytes;
                    return true;
                }
            }

            metadata = null;
            return false;
        }
    }
}

using System.Text;
using Grpc.Core;

namespace Tecan.Sila2.Client
{
    /// <summary>
    /// Helper class to deal with client metadata
    /// </summary>
    public static class ClientMetadata
    {
        /// <summary>
        /// Gets the header name for the given client metadata
        /// </summary>
        /// <param name="metadataIdentifier">The fully qualified identifier of the client metadata</param>
        /// <returns>The header name to be used</returns>
        public static string GetHeaderName(string metadataIdentifier)
        {
            var sb = new StringBuilder();
            sb.Append("sila-");
            foreach (var character in metadataIdentifier)
            {
                if (character == '/')
                {
                    sb.Append('-');
                }
                else
                {
                    sb.Append(char.ToLowerInvariant(character));
                }
            }
            sb.Append("-bin");
            return sb.ToString();
        }

        /// <summary>
        /// Translates the call information into gRPC call options
        /// </summary>
        /// <param name="clientCallInfo">The client call information for which a call option should be created</param>
        /// <returns>A gRPC call option</returns>
        public static CallOptions ToCallOptions( this IClientCallInfo clientCallInfo )
        {
            if( clientCallInfo?.Metadata == null || clientCallInfo.Metadata.Count == 0 )
            {
                return default;
            }

            var metadata = new Metadata();
            foreach( var option in clientCallInfo.Metadata )
            {
                metadata.Add( GetHeaderName( option.Key ), option.Value );
            }

            return new CallOptions( metadata );
        }
    }
}

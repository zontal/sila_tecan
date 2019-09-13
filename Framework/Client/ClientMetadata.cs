using System.Text;

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
    }
}

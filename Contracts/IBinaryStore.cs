using System.IO;
using System.Threading.Tasks;

namespace Tecan.Sila2.Binary
{
    /// <summary>
    /// Denotes the command for a binary store
    /// </summary>
    public interface IBinaryStore
    {
        /// <summary>
        /// Resolves the given identifier into a stream
        /// </summary>
        /// <param name="identifier">The binary storage identifier</param>
        /// <returns>A stream that contains the binary</returns>
        Stream ResolveStoredBinary(string identifier);
        
        /// <summary>
        /// Resolves the given identifier into a stream
        /// </summary>
        /// <param name="identifier">The binary storage identifier</param>
        /// <returns>A stream that contains the binary</returns>
        FileInfo ResolveStoredBinaryFile( string identifier );

        /// <summary>
        /// Decides whether the given binary should be stored
        /// </summary>
        /// <param name="length">The length of the data in bytes</param>
        /// <returns>True, if the binary should be stored, False if the binary can be inlined</returns>
        bool ShouldStoreBinary(long length);

        /// <summary>
        /// Stores the given binary from a stream
        /// </summary>
        /// <param name="data">The binary to store</param>
        /// <returns>A binary storage identifier</returns>
        string StoreBinary(Stream data);

        /// <summary>
        /// Stores the given binary from a file
        /// </summary>
        /// <param name="file">The binary to store</param>
        /// <returns>A binary storage identifier</returns>
        string StoreBinary(FileInfo file);

        /// <summary>
        /// Deletes the binary after usage
        /// </summary>
        /// <param name="identifier">The binary storage identifier</param>
        void Delete( string identifier );
    }
}
using System.IO;
// ReSharper disable InconsistentNaming

namespace Tecan.Sila2.Server.Binary
{
    /// <summary>
    /// An interface for a component that organizes files locally
    /// </summary>
    public interface IFileManagement
    {
        /// <summary>
        /// Deletes the file with the given binary storage identifier
        /// </summary>
        /// <param name="binaryTransferUUID">The unique binary storage identifier</param>
        /// <param name="isUploaded">True, if the file was uploaded, otherwise false</param>
        void Delete(string binaryTransferUUID, bool isUploaded);

        /// <summary>
        /// Opens the file with the given binary storage identifier for reading purposes
        /// </summary>
        /// <param name="binaryTransferUUID">The unique binary storage identifier</param>
        /// <returns>A stream</returns>
        Stream OpenRead(string binaryTransferUUID);

        /// <summary>
        /// Opens the file with the given binary storage identifier for writing purposes
        /// </summary>
        /// <param name="binaryTransferUUID">The unique binary storage identifier</param>
        /// <returns>A stream</returns>
        Stream OpenWrite(string binaryTransferUUID);

        /// <summary>
        /// Queries whether the file with the binary storage identifier exists in the file system
        /// </summary>
        /// <param name="binaryTransferUUID">The unique binary storage identifier</param>
        /// <returns>True, if the file exists, otherwise false</returns>
        bool Exists(string binaryTransferUUID);

        /// <summary>
        /// Creates a new file
        /// </summary>
        /// <param name="length">The suspected length of the file</param>
        /// <returns>The unique binary storage identifier</returns>
        string CreateNew(long length);

        /// <summary>
        /// Create an identifier for an existing file
        /// </summary>
        /// <param name="file">A file</param>
        /// <returns>The unique binary storage identifier</returns>
        string CreateIdentifier(FileInfo file);
    }
}

using System.IO;

namespace Tecan.Sila2.Server.Binary
{
    public interface IFileManagement
    {
        void Delete(string binaryTransferUUID, bool isUploaded);
        Stream OpenRead(string binaryTransferUUID);

        Stream OpenWrite(string binaryTransferUUID);

        bool Exists(string binaryTransferUUID);

        string CreateNew(long length);

        string CreateIdentifier(FileInfo file);
    }
}

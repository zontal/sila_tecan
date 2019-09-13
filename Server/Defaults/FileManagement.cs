using System.IO;
using Tecan.Sila2.Server.Binary;

namespace Tecan.Sila2.Server
{
    internal class FileManagement : IFileManagement
    {
        public string CreateIdentifier(FileInfo file)
        {
            var tempFile = Path.GetTempFileName();
            if (file.Exists)
            {
                file.CopyTo(tempFile);
            }

            return Path.GetFileName(tempFile);
        }

        public virtual string CreateNew(long length)
        {
            return Path.GetFileName(Path.GetTempFileName());
        }

        public void Delete(string binaryTransferUUID, bool isUploaded)
        {
            LocateFile(binaryTransferUUID).Delete();
        }

        public bool Exists(string binaryTransferUUID)
        {
            return LocateFile(binaryTransferUUID).Exists;
        }

        public Stream OpenRead(string binaryTransferUUID)
        {
            return LocateFile(binaryTransferUUID).OpenRead();
        }

        public Stream OpenWrite(string binaryTransferUUID)
        {
            return LocateFile(binaryTransferUUID).OpenWrite();
        }

        protected virtual FileInfo LocateFile(string binaryTransferUUID)
        {
            return new FileInfo(Path.Combine(Path.GetTempPath(), binaryTransferUUID));
        }
    }
}

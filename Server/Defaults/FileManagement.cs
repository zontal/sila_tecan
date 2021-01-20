using System.IO;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Server.Binary;

namespace Tecan.Sila2.Server
{
    internal class FileManagement : IFileManagement
    {
        public string CreateIdentifier(FileInfo file)
        {
            if( file.DirectoryName != Path.GetTempPath() )
            {
                var tempFile = Path.GetTempFileName();
                if( file.Exists )
                {
                    file.CopyTo( tempFile, true );
                }

                return Path.GetFileName( tempFile );
            }
            else
            {
                return file.Name;
            }
        }

        public virtual string CreateNew(long length)
        {
            return Path.GetFileName(Path.GetTempFileName());
        }

        public void Delete(string binaryTransferUUID, bool isUploaded)
        {
            LocateFileAndCheck( binaryTransferUUID).Delete();
        }

        public FileInfo Locate( string binaryTransferUUID )
        {
            return LocateFile( binaryTransferUUID );
        }

        public bool Exists(string binaryTransferUUID)
        {
            return LocateFile(binaryTransferUUID).Exists;
        }

        public Stream OpenRead(string binaryTransferUUID)
        {
            return LocateFileAndCheck( binaryTransferUUID).OpenRead();
        }

        public Stream OpenWrite(string binaryTransferUUID)
        {
            return LocateFileAndCheck( binaryTransferUUID).OpenWrite();
        }

        protected virtual FileInfo LocateFile(string binaryTransferUUID)
        {
            return new FileInfo(Path.Combine(Path.GetTempPath(), binaryTransferUUID));
        }

        private FileInfo LocateFileAndCheck(string binaryTransferUuid)
        {
            var file = LocateFile( binaryTransferUuid );
            if(file.Exists)
            {
                return file;
            }
            else
            {
                throw new InvalidBinaryTransferIdException( "The provided binary transfer Uuid is invalid.", binaryTransferUuid );
            }
        }
    }
}

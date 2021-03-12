using System.IO;
using Tecan.Sila2.Binary;
using Tecan.Sila2.Server.Binary;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes a class that stores any uploaded files into the local file system, per default into the temp folder
    /// </summary>
    public class FileManagement : IFileManagement
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
        public virtual string CreateNew(long length)
        {
            return Path.GetFileName(Path.GetTempFileName());
        }

        /// <inheritdoc />
        public void Delete(string binaryTransferUUID, bool isUploaded)
        {
            LocateFileAndCheck( binaryTransferUUID).Delete();
        }

        /// <inheritdoc />
        public FileInfo Locate( string binaryTransferUUID )
        {
            return LocateFile( binaryTransferUUID );
        }

        /// <inheritdoc />
        public bool Exists(string binaryTransferUUID)
        {
            return LocateFile(binaryTransferUUID).Exists;
        }

        /// <inheritdoc />
        public Stream OpenRead(string binaryTransferUUID)
        {
            return LocateFileAndCheck( binaryTransferUUID).OpenRead();
        }

        /// <inheritdoc />
        public Stream OpenWrite(string binaryTransferUUID)
        {
            return LocateFileAndCheck( binaryTransferUUID).OpenWrite();
        }

        /// <inheritdoc />
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

using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Binary
{
    /// <summary>
    /// Denotes the error that a binary download failed
    /// </summary>
    public class BinaryDownloadFailedException : BinaryTransferException
    {
        /// <summary>
        /// Initializes a new binary transfer exception
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="binaryStorageId">The binary storage id</param>
        public BinaryDownloadFailedException( string message, string binaryStorageId ) : base( message, binaryStorageId )
        {
        }
    }
}

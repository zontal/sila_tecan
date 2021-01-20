using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Binary
{
    /// <summary>
    /// Denotes the error that a binary upload failed
    /// </summary>
    public class BinaryUploadFailedException : BinaryTransferException
    {
        /// <summary>
        /// Initializes a new binary transfer exception
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="binaryStorageId">The binary storage id</param>
        public BinaryUploadFailedException( string message, string binaryStorageId ) : base( message, binaryStorageId )
        {
        }
    }
}

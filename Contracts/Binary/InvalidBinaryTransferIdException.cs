using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Binary
{
    /// <summary>
    /// Denotes the error that the binary transfer UUID used was invalid
    /// </summary>
    public class InvalidBinaryTransferIdException : BinaryTransferException
    {
        /// <summary>
        /// Initializes a new binary transfer exception
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="binaryStorageId">The binary storage id</param>
        public InvalidBinaryTransferIdException( string message, string binaryStorageId ) : base( message, binaryStorageId )
        {
        }
    }
}

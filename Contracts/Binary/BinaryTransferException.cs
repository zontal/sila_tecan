using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Binary
{
    /// <summary>
    /// Denotes an exception in the binary transfer
    /// </summary>
    public class BinaryTransferException : Exception
    {
        /// <summary>
        /// Initializes a new binary transfer exception
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="binaryStorageId">The binary storage id</param>
        public BinaryTransferException(string message, string binaryStorageId) : base(message)
        {
            Data.Add( nameof( BinaryStorageId ), binaryStorageId );
        }

        /// <summary>
        /// Gets the binary storage id connected with this error
        /// </summary>
        public string BinaryStorageId => (string)Data[nameof( BinaryStorageId )];
    }
}

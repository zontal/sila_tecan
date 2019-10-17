using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes an error that has happened during execution of a SiLA2 command
    /// </summary>
    public class SilaException : Exception
    {
        /// <inheritdoc />
        public SilaException( string message ) : base(message)
        {
        }

        /// <inheritdoc />
        public SilaException( string message, Exception innerException ) : base( message, innerException )
        {
        }
    }
}

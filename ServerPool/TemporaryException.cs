using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.ServerPooling
{
    /// <summary>
    /// Denotes an exception temporarily used by the SDK in order to indicate a SiLA Error (wraps an error into an exception)
    /// </summary>
    public class TemporaryException : Exception
    {
        /// <summary>
        /// The plain error as signaled by the server
        /// </summary>
        public SiLAErrorDto Error
        {
            get;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="error">The SiLA Error that should be wrapped</param>
        public TemporaryException(SiLAErrorDto error)
        {
            Error = error;
        }
    }
}

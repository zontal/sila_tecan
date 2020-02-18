using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a defined error
    /// </summary>
    public class DefinedErrorException : Exception
    {
        /// <summary>
        /// The fully qualified identifier of the error
        /// </summary>
        public string ErrorIdentifier
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="errorIdentifier">The fully qualified identifier of the error</param>
        /// <param name="message">The exception message</param>
        public DefinedErrorException( string errorIdentifier, string message ) : base( message )
        {
            ErrorIdentifier = errorIdentifier;
        }
    }
}

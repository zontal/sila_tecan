using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// An exception denoting a validation error
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Gets the parameter that failed the validation
        /// </summary>
        public string Parameter
        {
            get;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="parameter">The parameter identifier</param>
        public ValidationException( string message, string parameter ) : base(message)
        {
            Parameter = parameter;
        }
    }
}

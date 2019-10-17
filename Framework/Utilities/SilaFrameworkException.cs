using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes an exception against the basic SiLA2 framework
    /// </summary>
    public class SilaFrameworkException : Exception
    {
        /// <summary>
        /// Gets the type of the error
        /// </summary>
        public FrameworkErrorType ErrorType
        {
            get;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="errorType">The type of the error</param>
        public SilaFrameworkException( FrameworkErrorType errorType ) : base( GetErrorMessage( errorType ) )
        {
            ErrorType = errorType;
        }

        private static string GetErrorMessage( FrameworkErrorType errorType )
        {
            switch(errorType)
            {
                case FrameworkErrorType.CommandExecutionNotAccepted:
                    return "The server refused to execute the command.";
                case FrameworkErrorType.CommandExecutionNotFinished:
                    return "The command is not finished, yet.";
                case FrameworkErrorType.InvalidCommandExecutionUuid:
                    return "The provided command identifier does not correspond to a command.";
                default:
                    return "An unknown error occurred.";
            }
        }
    }
}

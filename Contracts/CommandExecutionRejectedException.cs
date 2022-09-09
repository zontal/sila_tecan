using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2
{
    /// <summary>
    /// Class to represent the error that a server has rejected the execution of a command
    /// </summary>
    public class CommandExecutionRejectedException : Exception
    {
        /// <inheritdoc />
        public CommandExecutionRejectedException() : base( "The execution of the requested command was not accepted" )
        {
        }

        /// <inheritdoc />
        public CommandExecutionRejectedException( string message ) : base( message )
        {
        }

    }
}

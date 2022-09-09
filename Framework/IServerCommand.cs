using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a command that is executed on the server
    /// </summary>
    public interface IServerCommand : IObservableCommand
    {
        /// <summary>
        /// Gets or sets the command identifier
        /// </summary>
        string CommandExecutionUuid
        {
            get; set;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes the execution of an observable command
    /// </summary>
    public class ObservableCommandExecution
    {
        /// <summary>
        /// Initializes a new observable command execution
        /// </summary>
        /// <param name="commandExecutionUUID">The unique identifier of the command execution</param>
        /// <param name="commandIdentifier">The unique identifier of the command that is executed</param>
        /// <param name="command">The running command instance</param>
        /// <param name="interceptions">The interceptions applied to this command execution</param>
        public ObservableCommandExecution( string commandExecutionUUID, string commandIdentifier, IObservableCommand command, IEnumerable<IRequestInterception> interceptions )
        {
            CommandExecutionID = commandExecutionUUID;
            CommandIdentifier = commandIdentifier;
            Interceptions = interceptions;
            Command = command;
        }

        /// <summary>
        /// Gets the running command
        /// </summary>
        public IObservableCommand Command
        {
            get;
        }

        /// <summary>
        /// Gets the unique identifier of the command execution
        /// </summary>
        public string CommandExecutionID { get; }

        /// <summary>
        /// Gets the unique identifier of the command
        /// </summary>
        public string CommandIdentifier { get; }

        /// <summary>
        /// Gets or sets the expiration of the command
        /// </summary>
        public DateTime? Expiration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a collection of request interceptions applied to the command execution
        /// </summary>
        public IEnumerable<IRequestInterception> Interceptions
        {
            get;
        }

        /// <summary>
        /// Completes the command execution
        /// </summary>
        /// <param name="responseTask">The completed response task</param>
        public void Complete( Task responseTask )
        {
            if(Interceptions != null)
            {
                if(responseTask.Exception != null)
                {
                    foreach(var interception in Interceptions)
                    {
                        interception.ReportException( responseTask.Exception );
                    }
                }
                else
                {
                    foreach(var interception in Interceptions)
                    {
                        interception.ReportSuccess();
                    }
                }
            }
        }
    }
}

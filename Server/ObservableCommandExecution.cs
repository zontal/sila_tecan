using System;
using System.Collections.Generic;

namespace Tecan.Sila2.Server
{
    internal class ObservableCommandExecution
    {
        public ObservableCommandExecution(string commandExecutionUUID, string commandIdentifier, IObservableCommand command, IEnumerable<IRequestInterception> interceptions)
        {
            CommandExecutionID = commandExecutionUUID;
            CommandIdentifier = commandIdentifier;
            Interceptions = interceptions;
            Command = command;
        }

        public IObservableCommand Command
        {
            get;
        }

        public string CommandExecutionID { get; }
        public string CommandIdentifier { get; }

        public DateTime? Expiration
        {
            get;
            set;
        }

        public IEnumerable<IRequestInterception> Interceptions
        {
            get;
        }
    }
}

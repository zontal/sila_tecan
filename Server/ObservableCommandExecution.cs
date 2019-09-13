using System;
using System.Collections;
using System.Collections.Generic;

namespace Tecan.Sila2.Server
{
    internal class ObservableCommandExecution
    {
        public ObservableCommandExecution(string commandExecutionUUID, string commandIdentifier, IEnumerable<IRequestInterception> interceptions)
        {
            CommandExecutionID = commandExecutionUUID;
            CommandIdentifier = commandIdentifier;
            CurrentState = new ExecutionState
            {
                Status = ExecutionState.CommandStatus.Waiting,
                Progress = new RealDto(0.0),
                EstimatedRemainingTime = new DurationDto(TimeSpan.Zero)
            };
            Interceptions = interceptions;
        }

        public string CommandExecutionID { get; }
        public string CommandIdentifier { get; }
        public ExecutionState CurrentState { get; }

        public IEnumerable<IRequestInterception> Interceptions
        {
            get;
        }

        public object IntermediateGenerator { get; set; }
    }

    internal class ObservableCommandExecution<T> : ObservableCommandExecution
    {
        public ObservableCommandExecution(string commandExecutionUUID, string commandIdentifier, Func<T> resultFactory, IEnumerable<IRequestInterception> interceptions ) : base(commandExecutionUUID, commandIdentifier, interceptions)
        {
            ResultFactory = resultFactory;
        }

        public Func<T> ResultFactory { get; }
   
    }
}

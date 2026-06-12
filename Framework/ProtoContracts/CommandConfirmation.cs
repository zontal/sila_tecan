using System;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer object for a command confirmation
    /// </summary>
    [ProtoContract()]
    public class CommandConfirmation
    {
        /// <summary>
        /// Creates an empty data transfer object
        /// </summary>
        public CommandConfirmation()
        {
        }

        /// <summary>
        /// Creates a data transfer object for the given command execution identifier
        /// </summary>
        /// <param name="commandId">The command execution identifier</param>
        public CommandConfirmation( string commandId )
        {
            CommandId = new CommandExecutionUuid( commandId );
        }

        /// <summary>
        /// Creates a data transfer object for the given command execution identifier
        /// </summary>
        /// <param name="commandId">The command execution identifier</param>
        /// <param name="expectedDuration">The expected duration</param>
        public CommandConfirmation( string commandId, TimeSpan expectedDuration )
        {
            CommandId = new CommandExecutionUuid( commandId );
            LifetimeOfExecution = new DurationDto( expectedDuration );
        }

        /// <summary>
        /// Gets the command execution identifier
        /// </summary>
        [ProtoMember( 1, Name = "commandExecutionUUID" )]
        public CommandExecutionUuid CommandId { get; set; }

        /// <summary>
        /// Gets the expected lifetime of execution
        /// </summary>
        [ProtoMember( 2, Name = "lifetimeOfExecution" )]
        public DurationDto LifetimeOfExecution { get; set; }

    }
}

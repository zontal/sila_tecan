using System.ComponentModel;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer class for a command execution identifier
    /// </summary>
    [ProtoContract( Name = "CommandExecutionUUID" )]
    public class CommandExecutionUuid
    {
        /// <summary>
        /// Creates an empty data transfer object
        /// </summary>
        public CommandExecutionUuid() { }

        /// <summary>
        /// Creates a data transfer object for the given command execution identifier
        /// </summary>
        /// <param name="commandId"></param>
        public CommandExecutionUuid( string commandId )
        {
            CommandId = commandId;
        }

        /// <summary>
        /// Gets the command execution identifier
        /// </summary>
        [ProtoMember( 1, Name = "commandId" )]
        [DefaultValue( "" )]
        public string CommandId { get; set; } = "";

    }
}

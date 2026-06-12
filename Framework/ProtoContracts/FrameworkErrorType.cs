using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes the framework error type
    /// </summary>
    [ProtoContract()]
    public enum FrameworkErrorType
    {
        /// <summary>
        /// The server denied the execution of a command
        /// </summary>
        [ProtoEnum( Name = @"COMMAND_EXECUTION_NOT_ACCEPTED" )]
        CommandExecutionNotAccepted = 0,
        /// <summary>
        /// The execution identifier is invalid
        /// </summary>
        [ProtoEnum( Name = @"INVALID_COMMAND_EXECUTION_UUID" )]
        InvalidCommandExecutionUuid = 1,
        /// <summary>
        /// The command execution did not finish
        /// </summary>
        [ProtoEnum( Name = @"COMMAND_EXECUTION_NOT_FINISHED" )]
        CommandExecutionNotFinished = 2,
        /// <summary>
        /// The metadata provided is invalid
        /// </summary>
        [ProtoEnum( Name = @"INVALID_METADATA" )]
        InvalidMetadata = 3,
        /// <summary>
        /// No metadata is allowed for this request
        /// </summary>
        [ProtoEnum( Name = @"NO_METADATA_ALLOWED" )]
        NoMetadataAllowed = 4,
    }
}

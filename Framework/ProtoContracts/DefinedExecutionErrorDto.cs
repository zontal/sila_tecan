using System.ComponentModel;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer object for an execution error
    /// </summary>
    [ProtoContract()]
    public class DefinedExecutionErrorDto
    {
        /// <summary>
        /// Gets or sets the fully qualified identifier of the execution error
        /// </summary>
        [ProtoMember( 1 )]
        [DefaultValue( "" )]
        public string ErrorIdentifier { get; set; } = "";

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [ProtoMember( 2, Name = @"message" )]
        [DefaultValue( "" )]
        public string Message { get; set; } = "";

    }
}

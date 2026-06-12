using System.ComponentModel;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer object for a validation error
    /// </summary>
    [ProtoContract()]
    public class ValidationErrorDto
    {
        /// <summary>
        /// Gets or sets the parameter identifier that did not validate
        /// </summary>
        [ProtoMember( 1, Name = @"parameter" )]
        [DefaultValue( "" )]
        public string Parameter { get; set; } = "";

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [ProtoMember( 2, Name = @"message" )]
        [DefaultValue( "" )]
        public string Message { get; set; } = "";

    }
}

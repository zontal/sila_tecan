using System.ComponentModel;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer object for an undefined execution error
    /// </summary>
    [ProtoContract()]
    public class UndefinedExecutionErrorDto : IExtensible
    {
        private IExtension __pbn__extensionData;
        IExtension IExtensible.GetExtensionObject( bool createIfMissing )
            => Extensible.GetExtensionObject( ref __pbn__extensionData, createIfMissing );

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [ProtoMember( 1, Name = @"message" )]
        [DefaultValue( "" )]
        public string Message { get; set; } = "";

    }
}

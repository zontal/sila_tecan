using System.ComponentModel;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer type for a framework error
    /// </summary>
    [ProtoContract()]
    public class FrameworkErrorDto : IExtensible
    {
        private IExtension __pbn__extensionData;
        IExtension IExtensible.GetExtensionObject( bool createIfMissing )
            => Extensible.GetExtensionObject( ref __pbn__extensionData, createIfMissing );

        /// <summary>
        /// Gets or sets the type of error
        /// </summary>
        [ProtoMember( 1 )]
        public FrameworkErrorType Type { get; set; }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [ProtoMember( 2, Name = @"message" )]
        [DefaultValue( "" )]
        public string Message { get; set; } = "";


    }
}

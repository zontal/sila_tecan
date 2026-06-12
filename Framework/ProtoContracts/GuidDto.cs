using System;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The transfer object for a Guid
    /// </summary>
    [ProtoContract()]
    public class GuidDto : ISilaTransferObject<Guid>
    {
        /// <summary>
        /// Creates a new dto
        /// </summary>
        public GuidDto() { }

        /// <summary>
        /// Creates a new dto with the given content
        /// </summary>
        /// <param name="value">The inner value</param>
        public GuidDto(Guid value)
        {
            Value = value.ToString();
        }

        /// <summary>
        /// Creates a new dto with the given content
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public GuidDto(Guid value, IBinaryStore store) : this(value) { }

        /// <summary>
        /// The actual value
        /// </summary>
        [ProtoMember(1, Name = @"value", IsRequired = true)]
        [System.ComponentModel.DefaultValue("")]
        public string Value { get; set; } = "";

        /// <inheritdoc />
        public Guid Extract(IBinaryStore resolver)
        {
            if (Guid.TryParse(Value, out Guid guid))
            {
                return guid;
            }
            return Guid.Empty;
        }

        /// <inheritdoc />
        public string GetValidationErrors()
        {
            if (!Guid.TryParse(Value, out Guid _))
            {
                return $"'{Value}' is not a valid UUID";
            }
            return null;
        }
    }
}

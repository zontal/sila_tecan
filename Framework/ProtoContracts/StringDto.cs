using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The transfer object for a string
    /// </summary>
    [ProtoContract()]
    public class StringDto : ISilaTransferObject<string>
    {
        private const int MaxLength = 2048 * 1024;

        /// <summary>
        /// Creates a new dto
        /// </summary>
        public StringDto() { }

        /// <summary>
        /// Creates a new dto with the given string content
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public StringDto( string value, IBinaryStore store = null )
        {
            Value = value;
        }

        /// <summary>
        /// The actual value
        /// </summary>
        [ProtoMember( 1, Name = @"value", IsRequired = true)]
        [System.ComponentModel.DefaultValue( "" )]
        public string Value { get; set; } = "";

        /// <summary>
        /// Packages a string into a dto
        /// </summary>
        /// <param name="value">The string value</param>
        public static implicit operator StringDto( string value )
        {
            return new StringDto( value );
        }

        /// <summary>
        /// Extracts the underlying string
        /// </summary>
        /// <returns>The actual string value</returns>
        public string Extract( IBinaryStore resolver )
        {
            return Value;
        }

        /// <summary>
        /// Gets the validation errors for this dto
        /// </summary>
        /// <returns>null, because the type does not perform validation</returns>
        public string GetValidationErrors()
        {
            if (Value != null && Value.Length > MaxLength)
            {
                return "Strings in SiLA2 must be shorter than 2**21 characters";
            }
            return null;
        }

        /// <summary>
        /// Creates a dto for the given string
        /// </summary>
        /// <param name="value">The string value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static StringDto Create( string value, IBinaryStore store = null )
        {
            return value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }
    }
}

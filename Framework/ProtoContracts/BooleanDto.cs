using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer type for a boolean value
    /// </summary>
    [ProtoContract()]
    public class BooleanDto : ISilaTransferObject<bool>
    {
        /// <summary>
        /// Creates an empty instance
        /// </summary>
        public BooleanDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public BooleanDto( bool value, IBinaryStore store = null )
        {
            Value = value;
        }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public BooleanDto( bool? value, IBinaryStore store = null )
        {
            Value = value.GetValueOrDefault( false );
        }

        /// <summary>
        /// The actual value for this dto
        /// </summary>
        [ProtoMember( 1, Name = @"value", IsRequired = true)]
        public bool Value { get; set; }

        /// <summary>
        /// Encapsulates the given value into a dto
        /// </summary>
        /// <param name="value">The actual value</param>
        public static implicit operator BooleanDto( bool value )
        {
            return new BooleanDto( value );
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public bool Extract( IBinaryStore resolver )
        {
            return Value;
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static BooleanDto Create( bool value, IBinaryStore store = null )
        {
            return value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}

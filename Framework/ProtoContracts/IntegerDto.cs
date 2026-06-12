using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The transfer object for an integer
    /// </summary>
    [ProtoContract()]
    public class IntegerDto : ISilaTransferObject<long>
    {
        /// <summary>
        /// Creates a new integer dto
        /// </summary>
        public IntegerDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public IntegerDto( long value, IBinaryStore store = null )
        {
            Value = value;
        }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public IntegerDto( long? value, IBinaryStore store = null )
        {
            Value = value.GetValueOrDefault( 0 );
        }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public IntegerDto( ulong value, IBinaryStore store = null )
        {
            Value = (long)value;
        }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public IntegerDto( ulong? value, IBinaryStore store = null )
        {
            Value = (long)value.GetValueOrDefault( 0 );
        }

        /// <summary>
        /// The actual value for this dto
        /// </summary>
        [ProtoMember( 1, Name = @"value", IsRequired = true )]
        public long Value { get; set; }

        /// <summary>
        /// Converts the given integer value to a dto
        /// </summary>
        /// <param name="value">The actual value</param>
        public static implicit operator IntegerDto( long value )
        {
            return new IntegerDto( value );
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public long Extract( IBinaryStore resolver )
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
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static IntegerDto Create( long value, IBinaryStore store = null )
        {
            return new IntegerDto( value );
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static IntegerDto Create( int value, IBinaryStore store = null )
        {
            return new IntegerDto( value );
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static IntegerDto Create( long? value, IBinaryStore store = null )
        {
            return new IntegerDto( value );
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static IntegerDto Create( int? value, IBinaryStore store = null )
        {
            return new IntegerDto( value );
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static IntegerDto Create( ulong value, IBinaryStore store = null )
        {
            return new IntegerDto( value );
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static IntegerDto Create( uint value, IBinaryStore store = null )
        {
            return new IntegerDto( (long)value );
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static IntegerDto Create( ulong? value, IBinaryStore store = null )
        {
            return new IntegerDto( value );
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static IntegerDto Create( uint? value, IBinaryStore store = null )
        {
            return new IntegerDto( (long?)value );
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static int ExtractToInt32( IntegerDto value, IBinaryStore store = null )
        {
            return (int)value.Value;
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static long? ExtractToNullableInt64( IntegerDto value, IBinaryStore store = null )
        {
            return value?.Value;
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static int? ExtractToNullableInt32( IntegerDto value, IBinaryStore store = null )
        {
            return (int)value.Value;
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static ulong ExtractToUInt64( IntegerDto value, IBinaryStore store = null )
        {
            return (ulong)value.Value;
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static uint ExtractToUInt32( IntegerDto value, IBinaryStore store = null )
        {
            return (uint)value.Value;
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static ulong? ExtractToNullableUInt64( IntegerDto value, IBinaryStore store = null )
        {
            return (ulong)value.Value;
        }

        /// <summary>
        /// Creates a new dto for the given value
        /// </summary>
        /// <param name="value">The actual value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static uint? ExtractToNullableUInt32( IntegerDto value, IBinaryStore store = null )
        {
            return (uint)value.Value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}

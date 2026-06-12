using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer type for floating numbers
    /// </summary>
    [ProtoContract()]
    public class RealDto : ISilaTransferObject<double>
    {
        /// <summary>
        /// Creates an empty instance
        /// </summary>
        public RealDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public RealDto( double value, IBinaryStore store = null )
        {
            Value = value;
        }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public RealDto( double? value, IBinaryStore store = null )
        {
            Value = value.GetValueOrDefault( double.NaN );
        }

        /// <summary>
        /// The actual value for this dto
        /// </summary>
        [ProtoMember( 1, Name = @"value", IsRequired = true)]
        public double Value { get; set; }

        /// <summary>
        /// Encapsulates the given floating point into a dto
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator RealDto( double value )
        {
            return new RealDto( value );
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public double Extract( IBinaryStore resolver )
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
        /// Encapsulates the given floating point into a dto
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static RealDto Create( double value, IBinaryStore store = null )
        {
            return new RealDto( value );
        }

        /// <summary>
        /// Encapsulates the given floating point into a dto
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static RealDto Create( double? value, IBinaryStore store = null )
        {
            return new RealDto( value );
        }

        /// <summary>
        /// Encapsulates the given floating point into a dto
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static RealDto Create( float value, IBinaryStore store = null )
        {
            return new RealDto( value );
        }

        /// <summary>
        /// Encapsulates the given floating point into a dto
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store</param>
        /// <returns>The encapsulated value</returns>
        public static RealDto Create( float? value, IBinaryStore store = null )
        {
            return new RealDto( value );
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}

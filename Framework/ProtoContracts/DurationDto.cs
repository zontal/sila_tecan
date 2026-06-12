using System;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer type for a duration
    /// </summary>
    [ProtoContract()]
    public class DurationDto : ISilaTransferObject<TimeSpan>
    {
        /// <summary>
        /// Creates an empty transfer object
        /// </summary>
        public DurationDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public DurationDto( TimeSpan value, IBinaryStore store = null )
        {
            Nanos = (int)(value.Ticks % TimeSpan.TicksPerSecond) * 100;
            Seconds = (long)value.TotalSeconds;
        }

        /// <summary>
        /// Gets or sets the seconds
        /// </summary>
        [ProtoMember( 1, Name = @"seconds" )]
        public long Seconds { get; set; }

        /// <summary>
        /// Gets or sets the nanoseconds
        /// </summary>
        [ProtoMember( 2, Name = @"nanos" )]
        public int Nanos { get; set; }

        /// <summary>
        /// Encapsulates the given duration into a transfer object
        /// </summary>
        /// <param name="value">The actual duration</param>
        public static implicit operator DurationDto( TimeSpan value )
        {
            return new DurationDto( value );
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public TimeSpan Extract( IBinaryStore resolver )
        {
            return new TimeSpan( Seconds * TimeSpan.TicksPerSecond + (Nanos / 100) );
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            return null;
        }
    }
}

using System;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer type for a timezone
    /// </summary>
    [ProtoContract()]
    public class TimezoneDto : ISilaTransferObject<TimeSpan>
    {
        /// <summary>
        /// Creates an empty transfer object
        /// </summary>
        public TimezoneDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public TimezoneDto(TimeSpan value, IBinaryStore store = null)
        {
            Hours = value.Hours;
            if (value.Minutes < 0)
            {
                Hours--;
                Minutes = (uint)(value.Minutes + 60);
            }
            else
            {
                Minutes = (uint)value.Minutes;
            }
        }

        /// <summary>
        /// Gets or sets the offset hours
        /// </summary>
        [ProtoMember( 1, Name = @"hours" )]
        public int Hours { get; set; }

        /// <summary>
        /// Gets or sets the offset minutes
        /// </summary>
        [ProtoMember( 2, Name = @"minutes" )]
        public uint Minutes { get; set; }

        /// <summary>
        /// Wraps the given offset into a transfer object
        /// </summary>
        /// <param name="offset">The given offset</param>
        public static implicit operator TimezoneDto( TimeSpan offset )
        {
            return new TimezoneDto( offset );
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public TimeSpan Extract( IBinaryStore resolver )
        {
            return new TimeSpan( 0, Hours, (int)Minutes, 0 );
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            if ( Minutes >= 60 || Hours > 14 || Hours < -14 )
            {
                return "Invalid time zone";
            }
            if (Minutes != 0 && Hours == 14)
            {
                return "Invalid time zone";
            }
            return null;
        }

        /// <summary>
        /// Wraps the given offset into a transfer object
        /// </summary>
        /// <param name="value">The given offset</param>
        public static TimezoneDto Create( TimeSpan value )
        {
            return new TimezoneDto( value );
        }
    }
}

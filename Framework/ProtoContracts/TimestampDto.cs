using System;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer type for a timestamp
    /// </summary>
    [ProtoContract()]
    public class TimestampDto : ISilaTransferObject<DateTimeOffset>
    {
        /// <summary>
        /// Creates an empty data transfer object
        /// </summary>
        public TimestampDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public TimestampDto( DateTimeOffset value, IBinaryStore store = null )
        {
            Second = value.Second;
            Minute = value.Minute;
            Hour = value.Hour;
            Day = value.Day;
            Month = value.Month;
            Year = value.Year;
            Timezone = value.Offset;
            Millisecond = (uint)value.Millisecond;
        }

        /// <summary>
        /// Gets or sets the second
        /// </summary>
        [ProtoMember( 1, Name = @"second" )]
        public long Second { get; set; }

        /// <summary>
        /// Gets or sets the minute
        /// </summary>
        [ProtoMember( 2, Name = @"minute" )]
        public long Minute { get; set; }

        /// <summary>
        /// Gets or sets the hour
        /// </summary>
        [ProtoMember( 3, Name = @"hour" )]
        public long Hour { get; set; }

        /// <summary>
        /// Gets or sets the day
        /// </summary>
        [ProtoMember( 4, Name = @"day" )]
        public long Day { get; set; }

        /// <summary>
        /// Gets or sets the month
        /// </summary>
        [ProtoMember( 5, Name = @"month" )]
        public long Month { get; set; }

        /// <summary>
        /// Gets or sets the year
        /// </summary>
        [ProtoMember( 6, Name = @"year" )]
        public long Year { get; set; }

        /// <summary>
        /// Gets or sets the timezone
        /// </summary>
        [ProtoMember(7, Name = @"timezone", IsRequired = true)]
        public TimezoneDto Timezone { get; set; }

        /// <summary>
        /// Gets or sets the milliseconds
        /// </summary>
        [ProtoMember(8, Name = @"millisecond")]
        public uint Millisecond { get; set; }

        /// <summary>
        /// Encapsulates the given timestamp into a dto
        /// </summary>
        /// <param name="value">The actual timestamp</param>
        public static implicit operator TimestampDto( DateTimeOffset value )
        {
            return new TimestampDto( value );
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public DateTimeOffset Extract( IBinaryStore resolver )
        {
            return new DateTimeOffset( (int)Year, (int)Month, (int)Day, (int)Hour, (int)Minute, (int)Second, (int)Millisecond, Timezone?.Extract( resolver ) ?? default );
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            if (Year < 1 || Year > 9999)
            {
                return "Year must be between 1 and 9999";
            }
            if (Month == 0 || Month > 12 || Day == 0 || Day > DateTime.DaysInMonth((int)Year, (int)Month))
            {
                return $"{Day}/{Month}/{Year} does not represent a valid date.";
            }
            if (Second >= 60 || Minute >= 60 || Millisecond >= 1000)
            {
                return "Invalid time";
            }
            if (Timezone == null)
            {
                return "Timezone is missing";
            }
            return Timezone.GetValidationErrors();
        }

        /// <summary>
        /// Encapsulates the given timestamp into a dto
        /// </summary>
        /// <param name="value">The actual timestamp</param>
        /// <param name="store">(ignored)</param>
        public static TimestampDto Create( DateTimeOffset value, IBinaryStore store = null )
        {
            return value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Extract( null ).ToString();
        }
    }
}

using System;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// The data transfer object for a date
    /// </summary>
    [ProtoContract()]
    public class DateDto : ISilaTransferObject<DateTimeOffset>
    {
        /// <summary>
        /// Creates an empty instance
        /// </summary>
        public DateDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public DateDto( DateTimeOffset value, IBinaryStore store = null )
        {
            Day = (uint)value.Day;
            Month = (uint)value.Month;
            Year = (uint)value.Year;
            Timezone = value.Offset;
        }

        /// <summary>
        /// Gets or sets the day
        /// </summary>
        [ProtoMember( 1, Name = @"day" )]
        public uint Day { get; set; }

        /// <summary>
        /// Gets or sets the month
        /// </summary>
        [ProtoMember( 2, Name = @"month" )]
        public uint Month { get; set; }

        /// <summary>
        /// Gets or sets the year
        /// </summary>
        [ProtoMember( 3, Name = @"year")]
        public ulong Year { get; set; }

        /// <summary>
        /// Gets or sets the timezone
        /// </summary>
        [ProtoMember(4, Name = @"timezone", IsRequired = true)]
        public TimezoneDto Timezone { get; set; }

        /// <summary>
        /// Encapsulates a given date into a data transfer object
        /// </summary>
        /// <param name="value">The actual date</param>
        public static implicit operator DateDto( DateTimeOffset value )
        {
            return new DateDto( value );
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public DateTimeOffset Extract( IBinaryStore resolver )
        {
            return new DateTimeOffset( (int)Year, (int)Month, (int)Day, 0, 0, 0, Timezone.Extract( resolver ) );
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
            if ( Month == 0 || Month > 12 || Day == 0 || Day > DateTime.DaysInMonth((int)Year, (int)Month))
            {
                return $"{Day}/{Month}/{Year} does not represent a valid date.";
            }
            if (Timezone == null)
            {
                return "Timezone is missing";
            }
            return Timezone.GetValidationErrors();
        }

        /// <summary>
        /// Encapsulates a given date into a data transfer object
        /// </summary>
        /// <param name="value">The actual date</param>
        /// <param name="store">(ignored)</param>
        public static DateDto Create( DateTimeOffset value, IBinaryStore store = null )
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

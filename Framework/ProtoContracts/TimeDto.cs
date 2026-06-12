using System;
using ProtoBuf;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes a data transfer object for a time
    /// </summary>
    [ProtoContract()]
    public class TimeDto : ISilaTransferObject<TimeSpan>
    {
        /// <summary>
        /// Creates a new data transfer object
        /// </summary>
        public TimeDto() { }

        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public TimeDto(TimeSpan value, IBinaryStore store = null)
        {
            if (value < TimeSpan.Zero)
            {
                var timezone = TimeSpan.FromHours(value.Hours);
                if (value - timezone < TimeSpan.Zero)
                {
                    timezone += TimeSpan.FromHours(-1);
                }
                Init(value - timezone);
                Timezone = timezone;
            }
            else
            {
                Init(value);
                Timezone = new TimezoneDto(TimeSpan.Zero);
            }
        }


        /// <summary>
        /// Creates a transfer object for the given inner value
        /// </summary>
        /// <param name="value">The inner value</param>
        /// <param name="store">A binary store to resolve binary data</param>
        public TimeDto(TimeWithTimezone value, IBinaryStore store = null)
        {
            if (value.Time < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("Time cannot be smaller than zero", nameof(value));
            }
            else
            {
                Init(value.Time);
                Timezone = new TimezoneDto(value.Timezone);
            }
        }

        private void Init(TimeSpan value)
        {
            Second = (uint)value.Seconds;
            Minute = (uint)value.Minutes;
            Hour = (uint)value.Hours;
            Millisecond = (uint)value.Milliseconds;
        }

        /// <summary>
        /// Gets or sets the second
        /// </summary>
        [ProtoMember( 1, Name = @"second")]
        public uint Second { get; set; }

        /// <summary>
        /// Gets or sets the minute
        /// </summary>
        [ProtoMember( 2, Name = @"minute" )]
        public uint Minute { get; set; }

        /// <summary>
        /// Gets or sets the hour
        /// </summary>
        [ProtoMember( 3, Name = @"hour" )]
        public uint Hour { get; set; }

        /// <summary>
        /// Gets or sets the timezone
        /// </summary>
        [ProtoMember( 4, Name = @"timezone", IsRequired = true)]
        public TimezoneDto Timezone { get; set; }

        /// <summary>
        /// Gets or sets the milliseconds
        /// </summary>
        [ProtoMember( 5, Name = @"millisecond")]
        public uint Millisecond { get; set; }

        /// <summary>
        /// Encapsulates the given time into a dto
        /// </summary>
        /// <param name="value">The actual time</param>
        public static implicit operator TimeDto( TimeSpan value )
        {
            return new TimeDto( value );
        }

        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public TimeSpan Extract( IBinaryStore resolver )
        {
            return new TimeSpan( 0, (int)Hour, (int)Minute, (int)Second, (int)Millisecond ) + Timezone.Extract( resolver );
        }


        /// <summary>
        /// Extracts the value from the dto
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <returns>The actual value</returns>
        public TimeWithTimezone ExtractToTimeWithTimezone(IBinaryStore resolver)
        {
            return new TimeWithTimezone( new TimeSpan(0, (int)Hour, (int)Minute, (int)Second, (int)Millisecond), Timezone.Extract(resolver));
        }


        /// <summary>
        /// Extracts the value from the dto into a time with time zone
        /// </summary>
        /// <param name="resolver">A resolver component to resolve binary data</param>
        /// <param name="parameterName">The name of the parameter for which the value should be extracted</param>
        /// <returns>The actual value</returns>
        public TimeWithTimezone TryExtractToTimeWithTimezone(IBinaryStore resolver, string parameterName)
        {
            try
            {
                return ExtractToTimeWithTimezone(resolver);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, parameterName, ex);
            }
        }

        /// <summary>
        /// Validates the contents of the current dto
        /// </summary>
        /// <returns>Null as no errors have been found</returns>
        public string GetValidationErrors()
        {
            if (Second >= 60 || Minute >= 60 || Millisecond >= 1000)
            {
                return "Invalid time";
            }
            if (Hour > 23)
            {
                return "Hour must be between 0 and 23";
            }
            if (Timezone == null)
            {
                return "Timezone is missing";
            }
            return Timezone.GetValidationErrors();
        }

        /// <summary>
        /// Encapsulates the given time into a dto
        /// </summary>
        /// <param name="value">The actual time</param>
        /// <param name="store">(ignored)</param>
        public static TimeDto Create( TimeSpan value, IBinaryStore store = null )
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

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Tecan.Sila2
{
    /// <summary>
    /// Represents a time with an associated timezone. 
    /// </summary>
    /// <param name="Time">Time of day, represented as a TimeSpan.</param>
    /// <param name="Timezone">Timezone offset to UTC time. For example, a value of +02:00 means that the time is 2 hours ahead of UTC time, while a value of -05:00 means that the time is 5 hours behind UTC time.</param>
    /// <remarks>This is used in cases where the timezone is relevant for the correct interpretation of the time, for example when scheduling a command to be executed at a specific time. The timezone is represented as an offset to UTC time.</remarks>
    public record struct TimeWithTimezone(TimeSpan Time, TimeSpan Timezone)
    {
        /// <summary>
        /// Creates a new instance of TimeWithTimezone with the given time and a timezone of UTC (0 offset)
        /// </summary>
        /// <param name="time">time of day</param>
        public TimeWithTimezone( TimeSpan time ) : this( time, TimeSpan.Zero )
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Time} (UTC{(Timezone >= TimeSpan.Zero ? "+" : "")}{Timezone})";
        }

        /// <summary>
        /// Widens a time span to a time with timezone
        /// </summary>
        /// <param name="time">the time</param>
        public static implicit operator TimeWithTimezone(TimeSpan time)
        {
            return new TimeWithTimezone(time);
        }

        /// <summary>
        /// Adds a time span to a time with timezone, resulting in a new time with timezone with the same timezone and the time span added to the time. This can be used to calculate a new time based on an existing time with timezone and an offset.
        /// </summary>
        /// <param name="time">the current time</param>
        /// <param name="offset">an offset</param>
        /// <returns></returns>
        public static TimeWithTimezone operator +( TimeWithTimezone time, TimeSpan offset )
        {
            return new TimeWithTimezone( time.Time + offset, time.Timezone );
        }
    }
}

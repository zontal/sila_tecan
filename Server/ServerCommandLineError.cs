using System;
using CommandLine;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Describes a problem found while parsing the server command line
    /// </summary>
    /// <remarks>
    /// The server parses its command line without reflection and reports problems with this type instead of the
    /// CommandLineParser error types, whose constructors are not public. <see cref="Error.Tag"/> carries the
    /// <see cref="ErrorType"/> CommandLineParser reports for the same input, so checks based on the tag such as
    /// <c>IsHelp()</c> and <c>IsVersion()</c> keep working.
    /// </remarks>
    public sealed class ServerCommandLineError : Error
    {
        internal ServerCommandLineError( ErrorType tag, bool stopsProcessing, string token, string shortName, string longName, string message )
            : base( tag, stopsProcessing )
        {
            Token = token;
            ShortName = shortName;
            LongName = longName;
            Message = message;
        }

        /// <summary>
        /// Gets the command line token that caused the error, if the error is not bound to an option
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// Gets the short name of the option the error refers to, or an empty string if the option has none
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// Gets the long name of the option the error refers to
        /// </summary>
        public string LongName { get; }

        /// <summary>
        /// Gets the message that is printed for this error in the help screen, or null if the error requests help or version information
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Returns the name of the CommandLineParser error type this error stands for, e.g. CommandLine.UnknownOptionError
        /// </summary>
        /// <returns>The name of the CommandLineParser error type</returns>
        public override string ToString()
        {
            return "CommandLine." + Tag;
        }

        /// <inheritdoc />
        public override bool Equals( object obj )
        {
            return obj is ServerCommandLineError other
                && Tag == other.Tag
                && string.Equals( Token, other.Token, StringComparison.Ordinal )
                && string.Equals( ShortName, other.ShortName, StringComparison.Ordinal )
                && string.Equals( LongName, other.LongName, StringComparison.Ordinal );
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Tag;
                hash = hash * 397 ^ ( Token?.GetHashCode() ?? 0 );
                hash = hash * 397 ^ ( ShortName?.GetHashCode() ?? 0 );
                return hash * 397 ^ ( LongName?.GetHashCode() ?? 0 );
            }
        }
    }
}

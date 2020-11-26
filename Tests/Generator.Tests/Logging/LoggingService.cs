using System.Collections.Generic;
using System.Linq;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Generator.Test.Logging
{
    [SilaFeature(true, "Infrastructure")]
    [SilaDisplayName("Logging")]
    [SilaDescription("Provides services for integration of device logging")]
    public interface ILoggingService
    {
        [Observable]
        [SilaIdentifier("ListenForLogEntries")]
        [SilaDisplayName("Listen for log entries")]
        [SilaDescription("Starts listening for log entries from the device")]
        IIntermediateObservableCommand<LogEntry> ListenForLogEntries();

        [MetadataType(typeof(string))]
        IRequestInterceptor LoggingActivity { get; }
    }

    [SilaDescription("Describes a log entry")]
    public class LogEntry
    {
        public LogEntry(string message, string channel, Severity severity)
        {
            Message = message;
            Channel = channel;
            Severity = severity;
        }

        public LogEntry(string message, string channel, Severity severity, IDictionary<string, string> data)
            : this(message, channel, severity, data?.Select(p => new LogEntryData(p.Key, p.Value))) { }

        public LogEntry(string message, string channel, Severity severity, IEnumerable<LogEntryData> data)
            : this(message, channel, severity)
        {
            Data = data?.ToList()?.AsReadOnly();
        }

        public string Message { get; }

        public string Channel { get; }

        public Severity Severity { get; }

        public ICollection<LogEntryData> Data { get; }
    }

    public class LogEntryData
    {
        public LogEntryData(string key, string value, IEnumerable<LogEntryData> children = null)
        {
            Key = key;
            Value = value;
            Children = children?.ToList()?.AsReadOnly();
        }

        public string Key { get; }

        public string Value { get; }

        public ICollection<LogEntryData> Children { get; }
    }

    /// <summary>
    ///     Possible severities of a message
    ///     Warning: DO NOT CHANGE THE INTEGER VALUES OF THE ENUM
    /// </summary>
    public enum Severity
    {
        /// <summary>The severity of the message is not defined</summary>
        None = 0,
        /// <summary>Message is just logged for debug purposes</summary>
        Debug = 1,
        /// <summary>
        ///     Message is logged to give the user an idea whats going on
        /// </summary>
        Info = 2,
        /// <summary>Detected unexpected behavior. May cause an error</summary>
        Warning = 4,
        /// <summary>
        ///     An error occurred but the application may continue
        /// </summary>
        Error = 8,
        /// <summary>
        ///     Critical error occurred. Application won't be able to continue
        /// </summary>
        Critical = 16, // 0x00000010
    }
}

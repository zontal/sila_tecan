using System;
using System.ComponentModel.Composition;
using NLog;

using Tecan.MAP.Contracts.Services.Logging;
using Tecan.MAP.Contracts.Services.Logging.Configuration;

namespace SampleService
{

    [Export(typeof(ILoggingChannelFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class NLogLogging : ILoggingChannelFactory, ILoggingChannel
    {

        public void Dispose() { }

        public ILoggingChannel CreateChannel(Type type) => this;

        public LoggingConfiguration Configuration => null;

        public void Log(Func<string> messageProvider, Severity severity = Severity.Debug)
        {
            Log(messageProvider, null, null, severity);
        }

        public void Log(Func<string> messageProvider, Action<IDataBuilder> dataProvider, Severity severity = Severity.Debug)
        {
            Log(messageProvider, null, dataProvider, severity);
        }

        public void Log(Func<string> messageProvider, ActivityId activityId, Severity severity = Severity.Debug)
        {
            Log(messageProvider, activityId, null, severity);
        }

        public void Log(Func<string> messageProvider, ActivityId activityId, Action<IDataBuilder> dataProvider, Severity severity = Severity.Debug)
        {
            var logger = LogManager.GetLogger(typeof(NLogLogging).FullName);
            var level = LogLevel.Info;

            switch (severity)
            {
                case Severity.Debug:
                    level = LogLevel.Debug;
                    break;
                case Severity.Info:
                    level = LogLevel.Info;
                    break;
                case Severity.Warning:
                    level = LogLevel.Warn;
                    break;
                case Severity.Error:
                    level = LogLevel.Error;
                    break;
                case Severity.Critical:
                    level = LogLevel.Fatal;
                    break;
            }

            var le = new LogEventInfo(level, logger.Name, null, messageProvider(), new object[] { });
            logger.Log(typeof(NLogLogging), le);

        }

        public void LogException(Func<string> messageProvider, Exception exception)
        {
            Log($"Exception: {messageProvider()}\n{exception}".ToString, Severity.Error);
        }
    }
}

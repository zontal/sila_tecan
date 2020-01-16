using System;
using Common.Logging;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.MAP.Contracts.Services.Logging.Configuration;

namespace Tecan.Sila2.Logging
{

    public class NLogLogging : ILoggingChannelFactory, ILoggingChannel
    {
        private static ILog s_log = LogManager.GetLogger<NLogLogging>();

        public NLogLogging()
        {
            Logging.SetupCommonLogging();
        }

        public void Dispose()
        {

        }

        public ILoggingChannel CreateChannel(Type type)
        {
            return this;
        }

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
            switch (severity)
            {
                case Severity.Debug:
                    s_log.Debug(messageProvider());
                    break;
                case Severity.Info:
                    s_log.Info(messageProvider());
                    break;
                case Severity.Warning:
                    s_log.Warn(messageProvider());
                    break;
                case Severity.Error:
                    s_log.Error(messageProvider());
                    break;
                case Severity.Critical:
                    s_log.Fatal(messageProvider());
                    break;
            }
        }

        public void LogException(Func<string> messageProvider, Exception exception)
        {
            s_log.Error($"Exception: {messageProvider()}\nStack Trace: {exception.StackTrace}");
        }
    }
}

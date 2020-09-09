using System;
using System.Collections.Generic;
using System.Text;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.MAP.Contracts.Services.Logging.Configuration;

namespace Client
{
    class ConsoleLogging : ILoggingChannelFactory, ILoggingChannel
    {
        public LoggingConfiguration Configuration => null;

        public ILoggingChannel CreateChannel(Type type)
        {
            return this;
        }

        public void Dispose()
        {
        }

        public void Log(Func<string> messageProvider, Severity severity = Severity.Debug)
        {
            Log(messageProvider, null, null, severity);
        }

        public void Log(Func<string> messageProvider, Action<IDataBuilder> dataProvider, Severity severity = Severity.Debug)
        {
            Log(messageProvider, null, null, severity);
        }

        public void Log(Func<string> messageProvider, ActivityId activityId, Severity severity = Severity.Debug)
        {
            Log(messageProvider, null, null, severity);
        }

        public void Log(Func<string> messageProvider, ActivityId activityId, Action<IDataBuilder> dataProvider, Severity severity = Severity.Debug)
        {
            Console.WriteLine(messageProvider());
        }

        public void LogException(Func<string> messageProvider, Exception exception)
        {
            Console.Error.WriteLine(exception);
        }
    }
}

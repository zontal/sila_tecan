using System;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.MAP.Contracts.Services.Logging.Configuration;

namespace Tecan.Sila2.Server
{
    internal class ConsoleLogging : ILoggingChannelFactory, ILoggingChannel
    {
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
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case Severity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case Severity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case Severity.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case Severity.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }
            Console.WriteLine(messageProvider());
            Console.ResetColor();
        }

        public void LogException(Func<string> messageProvider, Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(messageProvider());
            Console.Error.WriteLine(exception);
            Console.ResetColor();
        }
    }
}

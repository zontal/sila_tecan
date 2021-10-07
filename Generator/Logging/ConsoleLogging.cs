using Common.Logging;
using Common.Logging.Factory;
using System;

namespace Tecan.Sila2.Generator.Logging
{
    internal class ConsoleLogging : AbstractLogger, ILoggerFactoryAdapter, ILog
    {
        private readonly LogLevel _minimumSeverity;

        public ConsoleLogging( LogLevel minimumSeverity )
        {
            _minimumSeverity = minimumSeverity;
        }

        public override bool IsTraceEnabled => _minimumSeverity <= LogLevel.Trace;

        public override bool IsDebugEnabled => _minimumSeverity <= LogLevel.Debug;

        public override bool IsErrorEnabled => _minimumSeverity <= LogLevel.Error;

        public override bool IsFatalEnabled => _minimumSeverity <= LogLevel.Fatal;

        public override bool IsInfoEnabled => _minimumSeverity <= LogLevel.Info;

        public override bool IsWarnEnabled => _minimumSeverity <= LogLevel.Warn;

        public ILog GetLogger( Type type )
        {
            return this;
        }

        public ILog GetLogger( string key )
        {
            return this;
        }

        protected override void WriteInternal( LogLevel level, object message, Exception exception )
        {
            switch(level)
            {
                case LogLevel.All:
                case LogLevel.Trace:
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogLevel.Warn:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case LogLevel.Fatal:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Off:
                    return;
            }
            Console.Error.WriteLine( message );
            if (exception != null)
            {
                Console.Error.WriteLine( exception );
            }
            Console.ResetColor();
        }
    }
}

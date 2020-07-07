using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.MAP.Contracts.Services.Logging.Configuration;

namespace Tecan.Sila2.Generator.Test
{
    internal class TestLoggingFactory : ILoggingChannelFactory, ILoggingChannel
    {
        public void Dispose()
        {
        }

        public ILoggingChannel CreateChannel( Type type )
        {
            return this;
        }

        public LoggingConfiguration Configuration => null;

        public void Log( Func<string> messageProvider, Severity severity = Severity.Debug )
        {
            Console.WriteLine(messageProvider?.Invoke());
        }

        public void Log( Func<string> messageProvider, Action<IDataBuilder> dataProvider, Severity severity = Severity.Debug )
        {
            Console.WriteLine( messageProvider?.Invoke() );
        }

        public void Log( Func<string> messageProvider, ActivityId activityId, Severity severity = Severity.Debug )
        {
            Console.WriteLine( messageProvider?.Invoke() );
        }

        public void Log( Func<string> messageProvider, ActivityId activityId, Action<IDataBuilder> dataProvider, Severity severity = Severity.Debug )
        {
            Console.WriteLine( messageProvider?.Invoke() );
        }

        public void LogException( Func<string> messageProvider, Exception exception )
        {
            Console.WriteLine( messageProvider?.Invoke() );
        }
    }
}

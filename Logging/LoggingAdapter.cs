using System;
using System.ComponentModel.Composition;
using Common.Logging;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.MAP.Contracts.Services.Logging.Configuration;

namespace Tecan.Sila2.Logging
{
    [Export(typeof(ILoggingChannelFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class LoggingAdapter : ILoggingChannelFactory
    {
        public LoggingAdapter()
        {
            Logging.SetupCommonLogging();
        }

        public void Dispose()
        {

        }

        public ILoggingChannel CreateChannel(Type type)
        {
            return new LogAdapter( LogManager.GetLogger( type ) );
        }

        public LoggingConfiguration Configuration => null;
    }
}

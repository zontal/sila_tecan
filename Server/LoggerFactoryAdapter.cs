using System;
using System.Collections.Generic;
using System.Text;
using Common.Logging;
using Tecan.MAP.Contracts.Services.Logging;

namespace Tecan.Sila2.Server
{
    internal class LoggerFactoryAdapter : ILoggerFactoryAdapter
    {
        private readonly ILoggingChannelFactory _loggingChannelFactory;

        public LoggerFactoryAdapter( ILoggingChannelFactory loggingChannelFactory )
        {
            _loggingChannelFactory = loggingChannelFactory;
        }

        public ILog GetLogger( Type type )
        {
            return new LoggerChannelAdapter( _loggingChannelFactory.CreateChannel( type ) );
        }

        public ILog GetLogger( string key )
        {
            return null;
        }
    }
}

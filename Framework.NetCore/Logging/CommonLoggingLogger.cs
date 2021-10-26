using Common.Logging;
using Common.Logging.Factory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Tecan.Sila2.Logging
{
    /// <summary>
    /// Denotes a proxy class that routes ASP.NET Core logging to Common.Logging
    /// </summary>
    public class CommonLoggingLogger : ILogger
    {
        private readonly ILog _inner;

        /// <summary>
        /// Creates a new logging channel based on the provided inner channel
        /// </summary>
        /// <param name="inner">The inner channel</param>
        public CommonLoggingLogger(ILog inner)
        {
            _inner = inner ?? new Common.Logging.Simple.NoOpLogger();
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>( TState state )
        {
            return _inner.NestedThreadVariablesContext.Push( state?.ToString() );
        }

        /// <inheritdoc />
        public bool IsEnabled( LogLevel logLevel )
        {
            switch(logLevel)
            {
                case LogLevel.Trace:
                    return _inner.IsTraceEnabled;
                case LogLevel.Debug:
                    return _inner.IsDebugEnabled;
                case LogLevel.Information:
                    return _inner.IsInfoEnabled;
                case LogLevel.Warning:
                    return _inner.IsWarnEnabled;
                case LogLevel.Error:
                    return _inner.IsErrorEnabled;
                case LogLevel.Critical:
                    return _inner.IsFatalEnabled;
                default:
                    return false;
            }
        }

        /// <inheritdoc />
        public void Log<TState>( LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter )
        {
            switch(logLevel)
            {
                case LogLevel.Trace:
                    _inner.Trace( formatter?.Invoke( state, exception ), exception );
                    break;
                case LogLevel.Debug:
                    _inner.Debug( formatter?.Invoke( state, exception ), exception );
                    break;
                case LogLevel.Information:
                    _inner.Info( formatter?.Invoke( state, exception ), exception );
                    break;
                case LogLevel.Warning:
                    _inner.Warn( formatter?.Invoke( state, exception ), exception );
                    break;
                case LogLevel.Error:
                    _inner.Error( formatter?.Invoke( state, exception ), exception );
                    break;
                case LogLevel.Critical:
                    _inner.Fatal( formatter?.Invoke( state, exception ), exception );
                    break;
                default:
                    break;
            }
        }
    }
}

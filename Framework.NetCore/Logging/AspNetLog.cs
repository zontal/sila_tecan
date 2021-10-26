using Common.Logging;
using Common.Logging.Factory;
using Common.Logging.Simple;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Tecan.Sila2.Logging
{
    /// <summary>
    /// Denotes a log implementation that routes to ASP.NET Core
    /// </summary>
    public class AspNetLog : ILog, INestedVariablesContext
    {
        private readonly ILogger _inner;
        private static readonly IVariablesContext NoOpVariablesContext = new NoOpVariablesContext();

        /// <summary>
        /// Creates a new log based on the provided inner log
        /// </summary>
        /// <param name="inner"></param>
        public AspNetLog(ILogger inner)
        {
            _inner = inner;
        }

        /// <inheritdoc />
        public bool IsTraceEnabled => _inner.IsEnabled(LogLevel.Trace);

        /// <inheritdoc />
        public bool IsDebugEnabled => _inner.IsEnabled(LogLevel.Debug);

        /// <inheritdoc />
        public bool IsErrorEnabled => _inner.IsEnabled(LogLevel.Error);

        /// <inheritdoc />
        public bool IsFatalEnabled => _inner.IsEnabled(LogLevel.Critical);

        /// <inheritdoc />
        public bool IsInfoEnabled => _inner.IsEnabled(LogLevel.Information);

        /// <inheritdoc />
        public bool IsWarnEnabled => _inner.IsEnabled(LogLevel.Warning);

        /// <inheritdoc />
        public IVariablesContext GlobalVariablesContext => NoOpVariablesContext;

        /// <inheritdoc />
        public IVariablesContext ThreadVariablesContext => NoOpVariablesContext;

        /// <inheritdoc />
        public INestedVariablesContext NestedThreadVariablesContext => this;

        /// <inheritdoc />
        public bool HasItems => false;

        private static string ExtractMessage( Action<FormatMessageHandler> formatMessageCallback )
        {
            string message = null;
            formatMessageCallback?.Invoke( ( format, args ) =>
            {
                message = string.Format( format, args );
                return message;
            } );
            return message;
        }

        private static string ExtractMessage( Action<FormatMessageHandler> formatMessageCallback, IFormatProvider formatProvider )
        {
            string message = null;
            formatMessageCallback?.Invoke( ( format, args ) =>
            {
                message = string.Format( formatProvider, format, args );
                return message;
            } );
            return message;
        }

        /// <inheritdoc />
        public void Debug( object message )
        {
            _inner.LogDebug( message?.ToString() );
        }

        /// <inheritdoc />
        public void Debug( object message, Exception exception )
        {
            _inner.LogDebug( exception, message?.ToString() );
        }

        /// <inheritdoc />
        public void Debug( Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogDebug( ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Debug( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogDebug( exception, ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Debug( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogDebug( ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void Debug( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogDebug( exception, ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void DebugFormat( string format, params object[] args )
        {
            _inner.LogDebug( format, args );
        }

        /// <inheritdoc />
        public void DebugFormat( string format, Exception exception, params object[] args )
        {
            _inner.LogDebug( exception, format, args );
        }

        /// <inheritdoc />
        public void DebugFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            _inner.LogDebug( string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void DebugFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            _inner.LogDebug( exception, string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void Error( object message )
        {
            _inner.LogError( message?.ToString() );
        }

        /// <inheritdoc />
        public void Error( object message, Exception exception )
        {
            _inner.LogError( exception, message?.ToString() );
        }

        /// <inheritdoc />
        public void Error( Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogError( ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Error( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogError( exception, ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Error( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogError( ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void Error( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogError( exception, ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void ErrorFormat( string format, params object[] args )
        {
            _inner.LogError( format, args );
        }

        /// <inheritdoc />
        public void ErrorFormat( string format, Exception exception, params object[] args )
        {
            _inner.LogError( exception, format, args );
        }

        /// <inheritdoc />
        public void ErrorFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            _inner.LogError( string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void ErrorFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            _inner.LogError( exception, string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void Fatal( object message )
        {
            _inner.LogCritical( message?.ToString() );
        }

        /// <inheritdoc />
        public void Fatal( object message, Exception exception )
        {
            _inner.LogCritical( exception, message?.ToString() );
        }

        /// <inheritdoc />
        public void Fatal( Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogCritical( ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Fatal( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogCritical( exception, ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Fatal( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogCritical( ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void Fatal( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogCritical( exception, ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void FatalFormat( string format, params object[] args )
        {
            _inner.LogCritical( format, args );
        }

        /// <inheritdoc />
        public void FatalFormat( string format, Exception exception, params object[] args )
        {
            _inner.LogCritical( exception, format, args );
        }

        /// <inheritdoc />
        public void FatalFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            _inner.LogCritical( string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void FatalFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            _inner.LogCritical( exception, string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void Info( object message )
        {
            _inner.LogInformation( message?.ToString() );
        }

        /// <inheritdoc />
        public void Info( object message, Exception exception )
        {
            _inner.LogInformation( exception, message?.ToString() );
        }

        /// <inheritdoc />
        public void Info( Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogInformation( ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Info( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogInformation( exception, ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Info( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogInformation( ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void Info( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogInformation( exception, ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void InfoFormat( string format, params object[] args )
        {
            _inner.LogInformation( format, args );
        }

        /// <inheritdoc />
        public void InfoFormat( string format, Exception exception, params object[] args )
        {
            _inner.LogInformation( exception, format, args );
        }

        /// <inheritdoc />
        public void InfoFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            _inner.LogInformation( string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void InfoFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            _inner.LogInformation( exception, string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void Trace( object message )
        {
            _inner.LogTrace( message?.ToString() );
        }

        /// <inheritdoc />
        public void Trace( object message, Exception exception )
        {
            _inner.LogTrace( exception, message?.ToString() );
        }

        /// <inheritdoc />
        public void Trace( Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogTrace( ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Trace( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogTrace( exception, ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Trace( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogTrace( ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void Trace( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogTrace( exception, ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void TraceFormat( string format, params object[] args )
        {
            _inner.LogTrace( format, args );
        }

        /// <inheritdoc />
        public void TraceFormat( string format, Exception exception, params object[] args )
        {
            _inner.LogTrace( exception, format, args );
        }

        /// <inheritdoc />
        public void TraceFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            _inner.LogTrace( string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void TraceFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            _inner.LogTrace( exception, string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void Warn( object message )
        {
            _inner.LogWarning( message?.ToString() );
        }

        /// <inheritdoc />
        public void Warn( object message, Exception exception )
        {
            _inner.LogWarning( exception, message?.ToString() );
        }

        /// <inheritdoc />
        public void Warn( Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogWarning( ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Warn( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogWarning( exception, ExtractMessage( formatMessageCallback ) );
        }

        /// <inheritdoc />
        public void Warn( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            _inner.LogWarning( ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void Warn( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            _inner.LogWarning( exception, ExtractMessage( formatMessageCallback, formatProvider ) );
        }

        /// <inheritdoc />
        public void WarnFormat( string format, params object[] args )
        {
            _inner.LogWarning( format, args );
        }

        /// <inheritdoc />
        public void WarnFormat( string format, Exception exception, params object[] args )
        {
            _inner.LogWarning( exception, format, args );
        }

        /// <inheritdoc />
        public void WarnFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            _inner.LogWarning( string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public void WarnFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            _inner.LogWarning( exception, string.Format( formatProvider, format, args ) );
        }

        /// <inheritdoc />
        public virtual IDisposable Push( string text )
        {
            return _inner.BeginScope( text );
        }

        /// <inheritdoc />
        public virtual string Pop()
        {
            Warn( "Pop is not properly supported. Use the Dispose pattern." );
            return null;
        }

        /// <inheritdoc />
        public virtual void Clear()
        {
            Warn( "Clear is not properly supported. Use the Dispose pattern." );
        }
    }
}

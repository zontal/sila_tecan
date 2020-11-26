using System;
using System.Globalization;
using System.Text;
using Common.Logging;
using Tecan.MAP.Contracts.Services.Logging;

namespace Tecan.Sila2.Server
{
    internal class LoggerChannelAdapter : ILog
    {
        private readonly ILoggingChannel _innerChannel;

        public LoggerChannelAdapter( ILoggingChannel loggingChannel )
        {
            _innerChannel = loggingChannel;
        }

        private static string ExtractLogMessage( Action<FormatMessageHandler> formatMessageCallback )
        {
            var sb = new StringBuilder();
            formatMessageCallback?.Invoke( ( format, args ) =>
            {
                sb.AppendFormat( format, args );
                return null;
            } );
            return sb.ToString();
        }

        public void Trace( object message )
        {
            _innerChannel.Log( message.ToString, Severity.Debug );
        }

        public void Trace( object message, Exception exception )
        {
            _innerChannel.LogException( message.ToString, exception );
        }

        public void TraceFormat( string format, params object[] args )
        {
            Trace( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void TraceFormat( string format, Exception exception, params object[] args )
        {
            Trace( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void TraceFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            Trace( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void TraceFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            Trace( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void Trace( Action<FormatMessageHandler> formatMessageCallback )
        {
            Trace( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Trace( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Trace( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Trace( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            Trace( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Trace( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Trace( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Debug( object message )
        {
            _innerChannel.Log( message.ToString, Severity.Debug );
        }

        public void Debug( object message, Exception exception )
        {
            _innerChannel.LogException( message.ToString, exception );
        }

        public void DebugFormat( string format, params object[] args )
        {
            Debug( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void DebugFormat( string format, Exception exception, params object[] args )
        {
            Debug( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void DebugFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            Debug( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void DebugFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            Debug( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void Debug( Action<FormatMessageHandler> formatMessageCallback )
        {
            Debug( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Debug( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Debug( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Debug( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            Debug( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Debug( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Debug( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Info( object message )
        {
            _innerChannel.Log( message.ToString, Severity.Info );
        }

        public void Info( object message, Exception exception )
        {
            _innerChannel.LogException( message.ToString, exception );
        }

        public void InfoFormat( string format, params object[] args )
        {
            Info( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void InfoFormat( string format, Exception exception, params object[] args )
        {
            Info( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void InfoFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            Info( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void InfoFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            Info( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void Info( Action<FormatMessageHandler> formatMessageCallback )
        {
            Info( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Info( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Info( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Info( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            Info( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Info( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Info( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Warn( object message )
        {
            _innerChannel.Log( message.ToString, Severity.Warning );
        }

        public void Warn( object message, Exception exception )
        {
            _innerChannel.LogException( message.ToString, exception );
        }

        public void WarnFormat( string format, params object[] args )
        {
            Warn( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void WarnFormat( string format, Exception exception, params object[] args )
        {
            Warn( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void WarnFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            Warn( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void WarnFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            Warn( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void Warn( Action<FormatMessageHandler> formatMessageCallback )
        {
            Warn( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Warn( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Warn( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Warn( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            Warn( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Warn( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Warn( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Error( object message )
        {
            _innerChannel.Log( message.ToString, Severity.Error );
        }

        public void Error( object message, Exception exception )
        {
            _innerChannel.LogException( message.ToString, exception );
        }

        public void ErrorFormat( string format, params object[] args )
        {
            Error( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void ErrorFormat( string format, Exception exception, params object[] args )
        {
            Error( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void ErrorFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            Error( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void ErrorFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            Error( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void Error( Action<FormatMessageHandler> formatMessageCallback )
        {
            Error( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Error( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Error( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Error( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            Error( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Error( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Error( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Fatal( object message )
        {
            _innerChannel.Log( message.ToString, Severity.Critical );
        }

        public void Fatal( object message, Exception exception )
        {
            _innerChannel.LogException( message.ToString, exception );
        }

        public void FatalFormat( string format, params object[] args )
        {
            Fatal( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void FatalFormat( string format, Exception exception, params object[] args )
        {
            Fatal( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void FatalFormat( IFormatProvider formatProvider, string format, params object[] args )
        {
            Fatal( string.Format( CultureInfo.InvariantCulture, format, args ) );
        }

        public void FatalFormat( IFormatProvider formatProvider, string format, Exception exception, params object[] args )
        {
            Fatal( string.Format( CultureInfo.InvariantCulture, format, args ), exception );
        }

        public void Fatal( Action<FormatMessageHandler> formatMessageCallback )
        {
            Fatal( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Fatal( Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Fatal( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public void Fatal( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback )
        {
            Fatal( ExtractLogMessage( formatMessageCallback ) );
        }

        public void Fatal( IFormatProvider formatProvider, Action<FormatMessageHandler> formatMessageCallback, Exception exception )
        {
            Fatal( ExtractLogMessage( formatMessageCallback ), exception );
        }

        public bool IsTraceEnabled => true;

        public bool IsDebugEnabled => true;

        public bool IsErrorEnabled => true;

        public bool IsFatalEnabled => true;

        public bool IsInfoEnabled => true;

        public bool IsWarnEnabled => true;

        public IVariablesContext GlobalVariablesContext => null;

        public IVariablesContext ThreadVariablesContext => null;

        public INestedVariablesContext NestedThreadVariablesContext => null;
    }
}

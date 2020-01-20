using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Common.Logging;
using Tecan.MAP.Contracts.Services.Logging;

namespace Tecan.Sila2.Logging
{
    internal class LogAdapter : ILoggingChannel, IDataBuilder
    {
        private readonly ILog _innerLog;

        public LogAdapter( ILog innerLog )
        {
            _innerLog = innerLog;
        }

        public void Log( Func<string> messageProvider, Severity severity = Severity.Debug )
        {
            Log( messageProvider, null, null, severity );
        }

        public void Log( Func<string> messageProvider, Action<IDataBuilder> dataProvider, Severity severity = Severity.Debug )
        {
            Log( messageProvider, null, dataProvider, severity );
        }

        public void Log( Func<string> messageProvider, ActivityId activityId, Severity severity = Severity.Debug )
        {
            Log( messageProvider, activityId, null, severity );
        }

        public void Log( Func<string> messageProvider, ActivityId activityId, Action<IDataBuilder> dataProvider, Severity severity = Severity.Debug )
        {
            dataProvider?.Invoke( this );
            switch(severity)
            {
                case Severity.Debug:
                    _innerLog.Debug( messageProvider() );
                    break;
                case Severity.Info:
                    _innerLog.Info( messageProvider() );
                    break;
                case Severity.Warning:
                    _innerLog.Warn( messageProvider() );
                    break;
                case Severity.Error:
                    _innerLog.Error( messageProvider() );
                    break;
                case Severity.Critical:
                    _innerLog.Fatal( messageProvider() );
                    break;
            }
        }

        public void LogException( Func<string> messageProvider, Exception exception )
        {
            _innerLog.Error( messageProvider?.Invoke(), exception );
        }

        public IDataBuilder Add( string key, IEnumerable allItems, IDictionary<string, string> dataAttributes = null )
        {
            _innerLog.GlobalVariablesContext?.Set(key, allItems);
            RegisterDataAttributes( dataAttributes );
            return this;
        }

        private void RegisterDataAttributes( IDictionary<string, string> dataAttributes )
        {
            if( dataAttributes != null && _innerLog.GlobalVariablesContext != null)
            {
                foreach( var dataAttribute in dataAttributes )
                {
                    _innerLog.GlobalVariablesContext.Set( dataAttribute.Key, dataAttribute.Value );
                }
            }
        }

        public IDataBuilder Add( AggregateException aggregateException, IDictionary<string, string> dataAttributes = null )
        {
            RegisterDataAttributes( dataAttributes );
            return this;
        }

        public IDataBuilder Add( Exception exception, IDictionary<string, string> dataAttributes = null )
        {
            RegisterDataAttributes( dataAttributes );
            return this;
        }

        public IDataBuilder Add( string key, object value, IDictionary<string, string> dataAttributes = null )
        {
            _innerLog.GlobalVariablesContext?.Set( key, value );
            RegisterDataAttributes( dataAttributes );
            return this;
        }

        public IDataBuilder Add( string key, string value, IDictionary<string, string> dataAttributes = null )
        {
            _innerLog.GlobalVariablesContext?.Set( key, value );
            RegisterDataAttributes( dataAttributes );
            return this;
        }
    }
}

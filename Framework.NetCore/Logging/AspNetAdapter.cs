using Common.Logging;
using Common.Logging.Factory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Logging
{
    /// <summary>
    /// Denotes a common logging adapter that routes to the ASP.NET Core logs
    /// </summary>
    public class AspNetAdapter : ILoggerFactoryAdapter
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="loggerFactory">The logger factory used by ASP.NET Core</param>
        public AspNetAdapter(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public ILog GetLogger( Type type )
        {
            if (_loggerFactory != null)
            {
                return Wrap( _loggerFactory.CreateLogger( type ) );
            }
            return null;
        }

        /// <inheritdoc />
        public ILog GetLogger( string key )
        {
            if (_loggerFactory != null)
            {
                return Wrap( _loggerFactory.CreateLogger( key ) );
            }
            return null;
        }

        private ILog Wrap(ILogger logger)
        {
            return new AspNetLog( logger );
        }
    }
}

using Common.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Logging
{
    /// <summary>
    /// Denotes a proxy that routes ASP.NET Core logging to Common.Logging
    /// </summary>
    public class CommonLoggingProvider : ILoggerProvider
    {
        /// <inheritdoc />
        public ILogger CreateLogger( string categoryName )
        {
            return new CommonLoggingLogger( LogManager.GetLogger( categoryName ) );
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}

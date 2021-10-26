using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Tecan.Sila2.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Denotes extension methods used for logging
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Adds Common.Logging as a logging provider to ASP.NET Core
        /// </summary>
        /// <param name="loggingBuilder">The logging builder</param>
        /// <returns>The logging builder for chaining purposes</returns>
        public static ILoggingBuilder AddCommonLogging(this ILoggingBuilder loggingBuilder)
        {
            return loggingBuilder.AddProvider( new CommonLoggingProvider() );
        }
    }
}

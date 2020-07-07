using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Generators;
using Tecan.Sila2.Generator.Logging;

namespace Tecan.Sila2.Generator.CommandLine
{
    /// <summary>
    /// Denotes the abstract base class for verbs
    /// </summary>
    public abstract class VerbBase : ICommandLineVerb
    {
        /// <summary>
        /// Sets the minimum severity of the log level. Allowed values are Debug, Info, Warning, Error.
        /// </summary>
        [Option("verbosity", Required = false, Default = Severity.Info, HelpText = "Sets the minimum severity of the log level. Allowed values are Debug, Info, Warning, Error.")]
        public Severity MinimumSeverity
        {
            get;
            set;
        }

        /// <summary>
        /// Set up the logging according to the command line
        /// </summary>
        /// <param name="container"></param>
        protected ILoggingChannel SetupLogging( CompositionContainer container )
        {
            if( container.GetExportedValueOrDefault<IDependencyInjectionGenerator>() == null )
            {
                container.ComposeExportedValue<IDependencyInjectionGenerator>( new MefGenerator() );
            }

            Program.MinimumSeverity = MinimumSeverity;
            var loggingFactory = container.GetExportedValueOrDefault<ILoggingChannelFactory>();
            if( loggingFactory == null )
            {
                loggingFactory = new ConsoleLogging( MinimumSeverity );
                container.ComposeExportedValue<ILoggingChannelFactory>( loggingFactory );
            }

            return loggingFactory.CreateChannel( GetType() );
        }

        /// <inheritdoc cref="ICommandLineVerb"/>
        public abstract void Execute( CompositionContainer container );
    }
}

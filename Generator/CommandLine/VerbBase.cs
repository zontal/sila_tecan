using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using CommandLine;
using Common.Logging;
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
        [Option("verbosity", Required = false, Default = LogLevel.Info, HelpText = "Sets the minimum severity of the log level. Allowed values are Debug, Info, Warning, Error.")]
        public LogLevel MinimumSeverity
        {
            get;
            set;
        }

        /// <summary>
        /// Set up the logging according to the command line
        /// </summary>
        /// <param name="container"></param>
        protected ILog SetupLogging( CompositionContainer container )
        {
            if( container.GetExportedValueOrDefault<IDependencyInjectionGenerator>() == null )
            {
                container.ComposeExportedValue<IDependencyInjectionGenerator>( new MefGenerator() );
            }
            LogManager.Adapter = new ConsoleLogging( MinimumSeverity );

            return LogManager.GetLogger( GetType() );
        }

        /// <inheritdoc cref="ICommandLineVerb"/>
        public abstract void Execute( CompositionContainer container );
    }
}

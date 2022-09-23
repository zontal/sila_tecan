using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using CommandLine;
using Common.Logging;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Generators;
using Tecan.Sila2.Generator.Helper;
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
        [Option( "verbosity", Required = false, Default = LogLevel.Info, HelpText = "Sets the minimum severity of the log level. Allowed values are Debug, Info, Warning, Error." )]
        public LogLevel MinimumSeverity
        {
            get;
            set;
        }

        /// <summary>
        /// If set, all interactive interrogations of this command are suppressed.
        /// </summary>
        [Option( 'b', "non-interactive", HelpText = "If set, all interactive interrogations of this command are suppressed.", Required = false )]
        public bool IsNonInteractive
        {
            get;
            set;
        }

        /// <summary>
        /// Path to a file that contains additional constraints
        /// </summary>
        [Option( "config-file", Required = false, HelpText = "Path to a file that contains additional constraints" )]
        public string ConfigFile { get; set; }

        /// <summary>
        /// Set up the logging according to the command line
        /// </summary>
        /// <param name="container"></param>
        protected ILog SetupLogging( CompositionContainer container )
        {
            LogManager.Adapter = new ConsoleLogging( MinimumSeverity );
            GeneratorSettings.IsInteractive = !IsNonInteractive && Environment.UserInteractive;
            var channel = LogManager.GetLogger( GetType() );
            LoadConfiguration( channel, container.GetExportedValue<IGeneratorConfigSource>() );
            return channel;
        }


        private void LoadConfiguration( ILog channel, IGeneratorConfigSource configRepo )
        {
            var configFile = FeatureDefinitionConfigHelper.LoadConfigMappingFile( ConfigFile );
            if(!string.IsNullOrEmpty( ConfigFile ) && configFile == null)
            {
                channel.Warn( "Feature definition config file could not be loaded." );
            }
            if(configFile != null)
            {
                configRepo.Add( configFile );
            }
        }

        /// <inheritdoc cref="ICommandLineVerb"/>
        public abstract void Execute( CompositionContainer container );
    }
}

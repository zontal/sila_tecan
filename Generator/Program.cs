using System;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using CommandLine;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator
{
    /// <summary>
    /// The main class of the code generator
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Sets the minimum severity of the log level. Allowed values are Debug, Info, Warning, Error.
        /// </summary>
        public static Severity MinimumSeverity
        {
            get;
            internal set;
        }

        private static CompositionContainer _container;

        private static void CreateCompositionContainer()
        {
            var directory = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory, "SilaGen.*.dll");
            _container = new CompositionContainer( new AggregateCatalog( directory, new AssemblyCatalog( typeof(Program).Assembly ) ) );
        }

        public static void Main(string[] args)
        {
            CreateCompositionContainer();

            var verbTypes = _container.GetExportedValues<ICommandLineVerb>()
                .Select(verb => verb.GetType())
                .Distinct()
                .ToArray();

            Parser.Default.ParseArguments(args, verbTypes)
                .WithParsed<ICommandLineVerb>(verb =>
                {
                    try
                    {
                        verb.Execute( _container );
                    }
                    catch( Exception e )
                    {
                        Console.Error.WriteLine( e.Message );
                        Environment.Exit( 1 );
                    }
                } );
        }
    }
}

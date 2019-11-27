using System;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using CommandLine;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator
{
    internal class Program
    {
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

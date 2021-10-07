using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using CommandLine;
using Common.Logging;
using Microsoft.Build.Locator;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator
{
    /// <summary>
    /// The main class of the code generator
    /// </summary>
    public class Program
    {

        private static CompositionContainer _container;

        private static void CreateCompositionContainer()
        {
            var catalogs = new List<ComposablePartCatalog>();
            var directory = new DirectoryCatalog( AppDomain.CurrentDomain.BaseDirectory, "SilaGen.*.dll" );
            var silaGenExtensions = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData ), "Tecan", "SilaGen" );
            catalogs.Add(directory);
            catalogs.Add(new AssemblyCatalog(typeof(Program).Assembly));
            if( Directory.Exists( silaGenExtensions ) )
            {
                catalogs.Add( new DirectoryCatalog( silaGenExtensions, "SilaGen.*.dll" ) );
            }
            _container = new CompositionContainer( new AggregateCatalog(catalogs) );
        }

        /// <summary>
        /// The entry point for SilaGen
        /// </summary>
        /// <param name="args">The commandline parameters</param>
        public static void Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();

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
                        LogManager.GetLogger<Program>().Error( "Code generation failed", e );
                        Environment.Exit( 1 );
                    }
                } );
        }
    }
}

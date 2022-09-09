using CommandLine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;
// ReSharper disable PossibleMultipleEnumeration

namespace Tecan.Sila2.Generator.CommandLine
{
    /// <summary>
    /// Describes the command to generate a feature
    /// </summary>
    [Export( typeof( ICommandLineVerb ) )]
    [PartCreationPolicy( CreationPolicy.NonShared )]
    [Verb( "generate-feature", HelpText = "Generates an XML file in the feature description language that contains the contents of a given interface" )]
    internal class GenerateFeatureVerb : VerbBase
    {
        /// <summary>
        /// The path to the assembly that should be scanned for interfaces
        /// </summary>
        [Value( 0, Required = true, HelpText = "The path to the assembly that should be scanned for interfaces" )]
        public string AssemblyPath { get; set; }

        /// <summary>
        /// The name of the interface. If this option is omitted, then a single SiLA interface is assumed.
        /// </summary>
        [Option( 'i', "interface", Required = false, HelpText = "The name of the interface. If this option is omitted, then a single SiLA interface is assumed." )]
        public string InterfaceName { get; set; }

        /// <summary>
        /// The path where the generated feature description should be stored.
        /// </summary>
        [Value( 1, HelpText = "The path where the generated feature description should be stored.", Required = true )]
        public string OutputPath { get; set; }

        /// <summary>
        /// The use case. If omitted, the use case is inferred from the namespace.
        /// </summary>
        [Option( 'u', "category", HelpText = "The category of the feature. If omitted, the category is inferred from the namespace." )]
        public string Category { get; set; }

        /// <summary>
        /// The originator of the feature. If this is omitted, it is inferred from the namespace.
        /// </summary>
        [Option( 'o', "originator", HelpText = "The originator of the feature. If this is omitted, it is inferred from the namespace." )]
        public string Originator { get; set; }

        /// <summary>
        /// The version of the given feature. If omitted, the version of the assembly is used.
        /// </summary>
        [Option( 'v', "version", HelpText = "The version of the given feature. If omitted, the version of the assembly is used." )]
        public string Version { get; set; }

        public override void Execute( CompositionContainer container )
        {
            var channel = SetupLogging( container );
            var namespaces = new HashSet<string>();
            var fullPath = Path.GetFullPath( AssemblyPath );
            var assembly = Assembly.LoadFrom( fullPath );
            var featureTypes = assembly.ExportedTypes.Where( t =>
                 t.IsInterface && !t.IsGenericType );

            var generatorConfig = FeatureDefinitionConfigHelper.LoadConfigMappingFile( ConfigFile );

            if(InterfaceName == null)
            {
                if(generatorConfig == null)
                {
                    featureTypes = featureTypes.Where( t => t.GetCustomAttribute<SilaFeatureAttribute>() != null );
                }
                else
                {
                    featureTypes = featureTypes.Where( t => generatorConfig.Feature.Any( f => f.Code == t.Name || f.Code == t.FullName ) );
                }
            }
            Type featureType;
            try
            {
                featureType = InterfaceName != null
                    ? featureTypes.FirstOrDefault( t => string.Equals( t.FullName, InterfaceName, StringComparison.Ordinal ) )
                      ?? featureTypes.Single( t => string.Equals( t.Name, InterfaceName, StringComparison.Ordinal ) )
                    : featureTypes.Single();
            }
            catch(InvalidOperationException)
            {
                channel.Error( $"Could not find interface {InterfaceName}" );
                return;
            }

            var config = container.GetExportedValue<IGeneratorConfigSource>();
            FeatureDefinitionConfigHelper.AssignStandardValues( featureType, config, Originator, Category, Version );

            var featureGenerator = container.GetExportedValue<IFeatureDefinitionGenerator>();
            var feature = featureGenerator.GenerateFeature( featureType, namespaces );
            channel.Info( $"Saving generated feature to {OutputPath}" );
            FeatureSerializer.Save( feature, Path.GetFullPath( OutputPath ) );
        }

    }
}

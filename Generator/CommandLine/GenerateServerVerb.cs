using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

#if NET472
using Microsoft.Build.Evaluation;
#endif

namespace Tecan.Sila2.Generator.CommandLine
{
    /// <summary>
    /// Describes the command to generate a server implementation
    /// </summary>
    [Export( typeof( ICommandLineVerb ) )]
    [PartCreationPolicy( CreationPolicy.NonShared )]
    [Verb( "generate-server", HelpText = "Generates the complete server that exposes a given interface as a SiLA feature" )]
    internal class GenerateServerVerb : VerbBase
    {
        /// <summary>
        /// The path to the assembly that should be scanned for interfaces
        /// </summary>
        [Value( 0, Required = true, HelpText = "The path to the assembly that should be scanned for interfaces" )]
        public string AssemblyPath { get; set; }

        /// <summary>
        /// The path to the C# project to which the generated provider should be added
        /// </summary>
        [Value( 1, Required = true, HelpText = "The path to the C# project to which the generated provider should be added" )]
        public string ProviderProject { get; set; }

        /// <summary>
        /// The root namespace in the C# project to which the generated provider should be added
        /// </summary>
        [Option( 'r', "root-namespace", Required = false, HelpText = "The root namespace in the C# project to which the generated provider should be added." )]
        public string RootNamespace { get; set; }

        /// <summary>
        /// The name of the interface. If this option is omitted, then a single SiLA interface is assumed.
        /// </summary>
        [Option( 'i', "interface", Required = false, HelpText = "The name of the interface. If this option is omitted, then a single SiLA interface is assumed." )]
        public string InterfaceName { get; set; }

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

        /// <summary>
        /// The path to the feature that shall be generated. Defaults to the identifier of the feature
        /// </summary>
        [Option( 'f', "feature", HelpText =
            "The path to the feature that shall be generated. Defaults to the identifier of the feature" )]
        public string FeaturePath { get; set; }

        /// <summary>
        /// Additional namespaces the provider needs to import, separated by semicolon (;)
        /// </summary>
        [Option( "import-namespaces", HelpText = "Additional namespaces the provider needs to import, separated by semicolon (;)", Required = false )]
        public string ImportedNamespaces { get; set; }

        /// <summary>
        /// The namespace for the newly generated provider. Defaults to a combination of originator and use case if omitted.
        /// </summary>
        [Option( 'n', "namespace", HelpText = "The namespace for the newly generated provider. Defaults to a combination of originator and use case if omitted.", Required = false )]
        public string Namespace { get; set; }

        /// <summary>
        /// The path where the DTOs should be generated to
        /// </summary>
        [Option( 'd', "dto-path", Required = false, Default = "Dtos.cs", HelpText = "The path where the DTOs should be generated to" )]
        public string DtoPath { get; set; } = "Dtos.cs";

        /// <summary>
        /// The path where the provider should be generated to
        /// </summary>
        [Option( 'p', "provider-path", Required = false, Default = "Provider.cs", HelpText = "The path where the provider should be generated to" )]
        public string ProviderPath { get; set; } = "Provider.cs";

        /// <summary>
        /// If set, also clients for the respective features are generated
        /// </summary>
        [Option( 'c', "client", Required = false, Default = false, HelpText = "If set, also clients for the respective features are generated" )]
        public bool Client { get; set; }

        /// <summary>
        /// If specified, features that do not specify whether they are draft or not will be set to draft.
        /// </summary>
        [Option( "draft", Required = false, HelpText = "If specified, features that do not specify whether they are draft or not will be set to draft." )]
        public bool IsDraft
        {
            get;
            set;
        }

        public override void Execute( CompositionContainer container )
        {
            var channel = SetupLogging( container );
            var rootNamespace = RootNamespace;
#if NET472
            if(Namespace == null && rootNamespace == null)
            {
                channel.Debug( "Finding namespace from project file" );
                var project = new Project( ProviderProject );
                rootNamespace = project.Properties.FirstOrDefault( p => p.Name == "RootNamespace" )?.EvaluatedValue;
            }
#endif
            if(Namespace == null && rootNamespace == null)
            {
                throw new Exception( "Either namespace or root namespace must be set if they cannot be obtained from the project file." );
            }

            var directory = Path.GetDirectoryName( Path.GetFullPath( ProviderProject ) );
            var assemblyDirectory = Path.GetDirectoryName( Path.GetFullPath( AssemblyPath ) );

            var assemblyResolver = CreateAssemblyResolver( assemblyDirectory );
            AppDomain.CurrentDomain.AssemblyResolve += assemblyResolver;

            var assembly = Assembly.LoadFrom( Path.GetFullPath( AssemblyPath ) );

            var featureTypes = assembly.ExportedTypes.Where( t =>
                 t.IsInterface && !t.IsGenericType );

            var configRepo = container.GetExportedValue<IGeneratorConfigSource>();
            if(string.IsNullOrEmpty( InterfaceName ))
            {
                featureTypes = featureTypes.Where( t => t.GetCustomAttribute<SilaFeatureAttribute>() != null || configRepo.GetFeatureSpec( t ) != null );
            }
            else
            {
                featureTypes = featureTypes.Where( t => string.Equals( InterfaceName, t.Name, StringComparison.OrdinalIgnoreCase ) || string.Equals( InterfaceName, t.FullName, StringComparison.OrdinalIgnoreCase ) );
            }
            var featureGenerator = container.GetExportedValue<IFeatureDefinitionGenerator>();

            bool foundFeature = false;
            foreach(var featureType in featureTypes)
            {
                channel.Info( $"Processing interface {featureType.FullName}" );

                foundFeature = true;
                // First, generate the feature definition
                var importNamespaces = new HashSet<string>();

                FeatureDefinitionConfigHelper.AssignStandardValues( featureType, configRepo, Originator, Category, Version );
                var feature = featureGenerator.GenerateFeature( featureType, importNamespaces );
                // ReSharper disable once AssignNullToNotNullAttribute
                var featurePath = Path.Combine( directory, FeaturePath ?? feature.Identifier + ".sila.xml" );
                channel.Info( "Saving feature" );
                FeatureSerializer.Save( feature, featurePath );
                // Second, generate the provider (server only)
                var generateProvider = new GenerateProviderVerb
                {
                    DtoPath = Path.Combine( directory, feature.Identifier, DtoPath ),
                    FeaturePath = featurePath,
                    ImportedNamespaces = AddImports( ImportedNamespaces, importNamespaces ),
                    Namespace = Namespace ?? rootNamespace + "." + feature.Identifier,
                    ProviderPath = Path.Combine( directory, feature.Identifier, ProviderPath ),
                    ServerOnly = !Client,
                    MinimumSeverity = MinimumSeverity
                };
                channel.Info( "Generate provider" );
                generateProvider.Execute( container );
            }

            if(!foundFeature)
            {
                if(InterfaceName != null)
                {
                    channel.Error( $"No interface with the name {InterfaceName} could be found." );
                }
                else
                {
                    channel.Warn( "No interface with SilaFeature attribute has been found." );
                }
            }
        }

        private ResolveEventHandler CreateAssemblyResolver( string assemblyDirectory )
        {
            return ( o, e ) =>
            {
                var commaPosition = e.Name.IndexOf( ',' );
                if(commaPosition != -1)
                {
                    var fileLocation = Path.Combine( assemblyDirectory, e.Name.Substring( 0, commaPosition ) + ".dll" );
                    if(File.Exists( fileLocation ))
                    {
                        return Assembly.LoadFrom( fileLocation );
                    }
                }
                return null;
            };
        }

        private static string AddImports( string importString, IEnumerable<string> imports )
        {
            foreach(var import in imports)
            {
                if(importString != null)
                {
                    if(importString.Contains( import + ";" ) || importString.EndsWith( import ))
                    {
                        continue;
                    }

                    importString += ";" + import;
                }
                else
                {
                    importString = import;
                }
            }

            return importString;
        }
    }
}

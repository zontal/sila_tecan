using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Build.Evaluation;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.CommandLine
{
    /// <summary>
    /// Describes the command to generate a server implementation
    /// </summary>
    [Export( typeof( ICommandLineVerb ) )]
    [PartCreationPolicy( CreationPolicy.NonShared )]
    [Verb( "generate-client", HelpText = "Generates a client for the given SiLA feature" )]
    internal class GenerateClientVerb : VerbBase
    {
        /// <summary>
        /// The path to the assembly that should be scanned for interfaces
        /// </summary>
        [Value( 0, Required = true, HelpText = "The path to the feature" )]
        public string FeaturePath { get; set; }

        /// <summary>
        /// The path to the C# project to which the generated provider should be added
        /// </summary>
        [Value( 1, Required = true, HelpText = "The path to the C# project to which the generated provider should be added" )]
        public string ProviderProject { get; set; }

        /// <summary>
        /// The namespace for the newly generated interface. Defaults to a combination of originator and use case if omitted.
        /// </summary>
        [Option( 'n', "namespace", HelpText = "The namespace for the newly generated interface. Defaults to a combination of originator and use case if omitted." )]
        public string Namespace { get; set; }

        /// <summary>
        /// An interface that the code generator should adjust to. If not set, the generator will create a new interface based on the feature.
        /// </summary>
        [Option('i', "adjust-to-interface", HelpText = "An interface that the code generator should adjust to. If not set, the generator will create a new interface based on the feature." )]
        public string AdjustToInterface { get; set; }

        /// <inheritdoc />
        public override void Execute( CompositionContainer container )
        {
            var channel = SetupLogging( container );
            var feature = FeatureSerializer.Load( Path.GetFullPath( FeaturePath ) );
            var directory = Path.GetDirectoryName( ProviderProject );
            if( string.IsNullOrEmpty( AdjustToInterface ) )
            {
                var unit = container.GetExportedValue<IInterfaceGenerator>().GenerateInterfaceUnit( feature, Namespace ?? feature.Namespace );
                CodeGenerationHelper.GenerateCSharp( unit, Path.Combine( directory, feature.Identifier, $"I{feature.Identifier}.cs" ) );
#if !NETCOREAPP2_1
                if( Namespace == null )
                {
                    channel.Log("Finding root namespace in project file".ToString);
                    var project = new Project( ProviderProject );
                    Namespace = project.GetPropertyValue( "RootNamespace" );
                }
#endif
            }
            else
            {
                var type = Type.GetType( AdjustToInterface, throwOnError: false );
#if !NETCOREAPP2_1
                if( type == null )
                {
                    channel.Log( "Finding output path and assembly name in project file".ToString );
                    var project = new Project( ProviderProject );
                    var target = project.GetPropertyValue( "OutputPath" );
                    var fileName = project.GetPropertyValue( "AssemblyName" );
                    target = Path.Combine( directory, target, fileName + ".dll" );
                    if( File.Exists( target ) )
                    {
                        var assembly = Assembly.LoadFrom( target );
                        type = assembly.GetType( AdjustToInterface );
                    }
                }
#endif
                if( type == null )
                {
                    throw new InvalidOperationException( $"The type {AdjustToInterface} could not be found" );
                }

                if( string.IsNullOrEmpty( Namespace ) )
                {
                    Namespace = type.Namespace;
                }

                // generate the feature. We are not interested in the feature itself, but want the side-effect that the generator registers deviations
                container.GetExportedValue<IFeatureDefinitionGenerator>().GenerateFeature( type, feature.Originator, feature.Category, feature.FeatureVersion );
            }

            var generateProvider = new GenerateProviderVerb()
            {
                ClientOnly = true,
                FeaturePath = FeaturePath,
                DtoPath = Path.Combine( directory, feature.Identifier, "Dtos.cs" ),
                ProviderPath = Path.Combine( directory, feature.Identifier, "Client.cs" ),
                Namespace = Namespace + "." + feature.Identifier
            };
            generateProvider.Execute(container);
        }
    }
}

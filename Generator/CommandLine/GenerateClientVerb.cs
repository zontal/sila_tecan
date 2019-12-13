using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
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
    internal class GenerateClientVerb : ICommandLineVerb
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

        /// <inheritdoc />
        public void Execute( CompositionContainer container )
        {
            var feature = FeatureSerializer.Load( Path.GetFullPath( FeaturePath ) );
            var unit = container.GetExportedValue<IInterfaceGenerator>().GenerateInterfaceUnit( feature, Namespace ?? feature.Namespace );
            var directory = Path.GetDirectoryName( ProviderProject );
            CodeGenerationHelper.GenerateCSharp( unit, Path.Combine(directory, feature.Identifier, $"I{feature.Identifier}.cs") );

#if !NETCOREAPP2_1
            if( Namespace == null )
            {
                var project = new Project( ProviderProject );
                Namespace = project.Properties.FirstOrDefault( p => p.Name == "RootNamespace" )?.EvaluatedValue;
            }
#endif

            var generateProvider = new GenerateProviderVerb()
            {
                ClientOnly = true,
                FeaturePath = FeaturePath,
                DtoPath = Path.Combine( directory, feature.Identifier, "Dtos.cs" ),
                ProviderPath = Path.Combine( directory, feature.Identifier, "Client.cs" ),
                Namespace = Namespace
            };
            generateProvider.Execute(container);
        }
    }
}

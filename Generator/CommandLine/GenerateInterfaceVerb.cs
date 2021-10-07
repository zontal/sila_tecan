using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using CommandLine;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;

namespace Tecan.Sila2.Generator.CommandLine
{
    /// <summary>
    /// Describes the command to generate an interface
    /// </summary>
    [Export(typeof(ICommandLineVerb))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Verb("generate-interface", HelpText = "Generates the interface for a given feature description")]
    internal class GenerateInterfaceVerb : VerbBase
    {
        /// <summary>
        /// The path to the feature from which an interface shall be generated
        /// </summary>
        [Value(0, Required = true, HelpText = "The path to the feature from which an interface shall be generated")]
        public string FeaturePath { get; set; }

        /// <summary>
        /// The namespace for the newly generated interface. Defaults to a combination of originator and use case if omitted.
        /// </summary>
        [Option('n', "namespace", HelpText = "The namespace for the newly generated interface. Defaults to a combination of originator and use case if omitted.")]
        public string Namespace { get; set; }

        /// <summary>
        /// The path where the interface should be generated
        /// </summary>
        [Value(1, Required = true, HelpText = "The path where the interface should be generated")]
        public string DestinationPath { get; set; }

        public override void Execute(CompositionContainer container)
        {
            var channel = SetupLogging( container );
            channel.Info("Loading feature" );
            var feature = FeatureSerializer.Load(Path.GetFullPath(FeaturePath));
            var unit = container.GetExportedValue<IInterfaceGenerator>().GenerateInterfaceUnit(feature, Namespace ?? feature.Namespace);
            channel.Info("Generate code for the interface");
            CodeGenerationHelper.GenerateCSharp(unit, DestinationPath);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using CommandLine;
using Tecan.Sila2.Generator.Contracts;
// ReSharper disable PossibleMultipleEnumeration

namespace Tecan.Sila2.Generator.CommandLine
{
    /// <summary>
    /// Describes the command to generate a feature
    /// </summary>
    [Export(typeof(ICommandLineVerb))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Verb("generate-feature", HelpText = "Generates an XML file in the feature description language that contains the contents of a given interface")]
    internal class GenerateFeatureVerb : ICommandLineVerb
    {
        /// <summary>
        /// The path to the assembly that should be scanned for interfaces
        /// </summary>
        [Value(0, Required = true, HelpText = "The path to the assembly that should be scanned for interfaces")]
        public string AssemblyPath { get; set; }

        /// <summary>
        /// The name of the interface. If this option is omitted, then a single SiLA interface is assumed.
        /// </summary>
        [Option('i', "interface", Required = false, HelpText = "The name of the interface. If this option is omitted, then a single SiLA interface is assumed.")]
        public string InterfaceName { get; set; }

        /// <summary>
        /// The path where the generated feature description should be stored.
        /// </summary>
        [Value(1, HelpText = "The path where the generated feature description should be stored.", Required = true)]
        public string OutputPath { get; set; }

        /// <summary>
        /// The use case. If omitted, the use case is inferred from the namespace.
        /// </summary>
        [Option('u', "usecase", HelpText = "The use case. If omitted, the use case is inferred from the namespace.")]
        public string UseCase { get; set; }

        /// <summary>
        /// The originator of the feature. If this is omitted, it is inferred from the namespace.
        /// </summary>
        [Option('o', "originator", HelpText = "The originator of the feature. If this is omitted, it is inferred from the namespace.")]
        public string Originator { get; set; }

        /// <summary>
        /// The version of the given feature. If omitted, the version of the assembly is used.
        /// </summary>
        [Option('v', "version", HelpText = "The version of the given feature. If omitted, the version of the assembly is used.")]
        public string Version { get; set; }

        /// <summary>
        /// Path to a file that contains additional constraints
        /// </summary>
        [Option('c', "constraints-file", Required = false, HelpText = "Path to a file that contains additional constraints")]
        public string ConstraintsFile { get; set; }

        public void Execute(CompositionContainer container)
        {
            var namespaces = new HashSet<string>();
            var fullPath = Path.GetFullPath(AssemblyPath);
            var assembly = Assembly.LoadFrom(fullPath);
            var featureTypes = assembly.ExportedTypes.Where(t =>
                t.IsInterface && !t.IsGenericType && t.GetCustomAttribute<SilaFeatureAttribute>() != null);
            var featureType = InterfaceName != null
                ? featureTypes.FirstOrDefault(t => string.Equals(t.FullName, InterfaceName, StringComparison.Ordinal)) ?? featureTypes.Single(t => string.Equals( t.Name, InterfaceName, StringComparison.Ordinal ) )
                : featureTypes.Single();
            var featureGenerator = container.GetExportedValue<IFeatureDefinitionGenerator>();
            var predefinedConstraints = LoadConstraintMappingFile(ConstraintsFile);
            var feature = featureGenerator.GenerateFeature(featureType, Originator, UseCase,
                Version, namespaces, predefinedConstraints);
            FeatureSerializer.Save(feature, Path.GetFullPath(OutputPath));
        }

        internal static IReadOnlyDictionary<string, Constraints> LoadConstraintMappingFile(string constraintsFile)
        {
            IReadOnlyDictionary<string, Constraints> predefinedConstraints = null;
            if (constraintsFile != null)
            {
                if (File.Exists(constraintsFile))
                {
                    var serializer = new XmlSerializer(typeof(ConstraintProvider));
                    using (var stream = File.OpenRead(constraintsFile))
                    {
                        var constraints = (ConstraintProvider) serializer.Deserialize(stream);
                        predefinedConstraints = constraints.Mapping?.ToDictionary(m => m.Element, m => m.Constraint);
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Constraints file {constraintsFile} could not be found.");
                }
            }

            return predefinedConstraints;
        }
    }
}

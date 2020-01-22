using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Tecan.Sila2.Generator.Contracts;
using Tecan.Sila2.Generator.Helper;
using Tecan.Sila2.Server;

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates features from a given interface
    /// </summary>
    [Export(typeof(IFeatureDefinitionGenerator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class FeatureDefinitionGenerator : IFeatureDefinitionGenerator
    {

        /// <summary>
        /// A registry to which any deviations from suggested rules are reported
        /// </summary>
        public ICodeNameRegistry NameRegistry { get; }

        private IEnumerable<IGeneratorHook> Hooks { get; }
        private readonly ITypeTranslationProvider _translationProvider;

        /// <summary>
        /// Creates a new feature definition generator for the provided registry
        /// </summary>
        /// <param name="nameRegistry">A component that accepts rename reports</param>
        /// <param name="translationProvider">A component that translates types between SiLA2 and .NET</param>
        /// <param name="hooks">Generator hooks</param>
        [ImportingConstructor]
        public FeatureDefinitionGenerator( ICodeNameRegistry nameRegistry, ITypeTranslationProvider translationProvider, [ImportMany] IEnumerable<IGeneratorHook> hooks )
        {
            NameRegistry = nameRegistry;
            Hooks = hooks;
            _translationProvider = translationProvider;
        }

        /// <inheritdoc />
        public Feature GenerateFeature( Type interfaceType, string originator = null, string category = null,
            string version = null, ICollection<string> namespaceCollector = null, IReadOnlyDictionary<string, Constraints> predefinedConstraints = null )
        {
            if(interfaceType == null) throw new ArgumentNullException( nameof( interfaceType ) );
            var annotation = interfaceType.GetCustomAttribute<SilaFeatureAttribute>();
            if(annotation == null)
                throw new ArgumentException( "Interface must be annotated as SiLA feature", nameof( interfaceType ) );

            var versionV = version == null ? interfaceType.Assembly.GetName().Version : Version.Parse( version );
            category ??= annotation.Category;
            AssignDefaultOriginatorAndUseCase( interfaceType, versionV, ref originator, ref category );

            Func<string, Constraints> constraintResolver = null;
            if( predefinedConstraints != null )
            {
                constraintResolver = origin =>
                {
                    if( predefinedConstraints.TryGetValue( origin, out var constraint ) )
                    {
                        return constraint;
                    }
                    else
                    {
                        Console.WriteLine($"No constraint for element {origin} found, creating only constraints based on annotations.");
                        return null;
                    }
                };
            }

            var context = new FeatureDefinitionGeneratorContext(NameRegistry, constraintResolver, _translationProvider);
            context.TryLoadDocumentation( interfaceType.Assembly );

            var (identifier, displayName, description) = context.ParseElementData( interfaceType, prefix: "I" );
            var items = context.GenerateFeature( interfaceType, namespaceCollector );

            var feature = new Feature
            {
                Description = description,
                DisplayName = displayName,
                Identifier = identifier,
                Items = items.ToArray(),
                SiLA2Version = "1.0",
                FeatureVersion = versionV.ToString( 2 ),
                MaturityLevel = annotation.IsDraft ? FeatureMaturityLevel.Draft : FeatureMaturityLevel.Verified,
                Category = category.ToLowerInvariant(),
                Originator = originator.ToLowerInvariant()
            };

            if(Hooks != null)
            {
                foreach(var hook in Hooks)
                {
                    hook?.OnFeatureGenerated( interfaceType, feature );
                }
            }

            return feature;
        }


        private static void AssignDefaultOriginatorAndUseCase( Type interfaceType, Version version,
            ref string originator,
            ref string useCase )
        {
            if(originator == null || useCase == null)
            {
                var ns = interfaceType.FullName.Substring( 0,
                    interfaceType.FullName.Length - interfaceType.Name.Length - 1 );
                var useCaseStartIndex = 0;
                var endIndex = ns.Length;
                var versionSuffix = "v" + version.Major.ToString();
                if(ns.EndsWith( versionSuffix ))
                {
                    endIndex -= versionSuffix.Length + 1;
                }

                if(useCase != null)
                {
                    useCaseStartIndex = ns.IndexOf( useCase, StringComparison.OrdinalIgnoreCase );
                    if(useCaseStartIndex == -1) originator = ns;
                }
                else if(originator != null)
                {
                    var originatorStartIndex = ns.IndexOf( originator, StringComparison.OrdinalIgnoreCase );
                    useCaseStartIndex = originatorStartIndex + originator.Length + 1;
                }
                else
                {
                    useCaseStartIndex = ns.LastIndexOf( ".", StringComparison.Ordinal ) + 1;
                }

                if(originator == null) originator = ns.Substring( 0, useCaseStartIndex - 1 );
                if(useCase == null) useCase = ns.Substring( useCaseStartIndex, endIndex - useCaseStartIndex );
            }

            originator = AsStandardLower( originator );
            useCase = AsStandardLower( useCase );
        }

        private static string AsStandardLower( string parameter )
        {
            var sb = new StringBuilder();
            foreach(var character in parameter)
            {
                if(char.IsLetter( character ))
                {
                    var lower = char.ToLowerInvariant( character );
                    if(lower >= 'a' && lower <= 'z')
                    {
                        sb.Append( lower );
                    }
                    else if(lower == 'ä')
                    {
                        sb.Append( "ae" );
                    }
                    else if(lower == 'ö')
                    {
                        sb.Append( "oe" );
                    }
                    else if(lower == 'ü')
                    {
                        sb.Append( "ue" );
                    }
                    else if(lower == 'ß')
                    {
                        sb.Append( "ss" );
                    }
                }
                else if(character == '.')
                {
                    sb.Append( character );
                }
            }
            return sb.ToString();
        }
    }
}

using Common.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Generates features from a given interface
    /// </summary>
    [Export( typeof( IFeatureDefinitionGenerator ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class FeatureDefinitionGenerator : IFeatureDefinitionGenerator
    {

        /// <summary>
        /// A registry to which any deviations from suggested rules are reported
        /// </summary>
        public ICodeNameRegistry NameRegistry { get; }

        private IEnumerable<IGeneratorHook> Hooks { get; }
        private readonly ITypeTranslationProvider _translationProvider;
        private readonly ILog _loggingChannel = LogManager.GetLogger<FeatureDefinitionGenerator>();
        private readonly IAmbiguityResolver _ambiguityResolver;
        private readonly IGeneratorConfigSource _generatorConfigSource;

        /// <summary>
        /// Creates a new feature definition generator for the provided registry
        /// </summary>
        /// <param name="nameRegistry">A component that accepts rename reports</param>
        /// <param name="translationProvider">A component that translates types between SiLA2 and .NET</param>
        /// <param name="hooks">Generator hooks</param>
        /// <param name="ambiguityResolver">A component that resolves ambiguous properties</param>
        /// <param name="configSource">A component from which generator specifications can be drawn</param>
        [ImportingConstructor]
        public FeatureDefinitionGenerator( ICodeNameRegistry nameRegistry, ITypeTranslationProvider translationProvider, IGeneratorConfigSource configSource,
            [ImportMany] IEnumerable<IGeneratorHook> hooks, IAmbiguityResolver ambiguityResolver )
        {
            NameRegistry = nameRegistry;
            Hooks = hooks;
            _translationProvider = translationProvider;
            _ambiguityResolver = ambiguityResolver;
            _generatorConfigSource = configSource;
        }

        /// <inheritdoc />
        public Feature GenerateFeature( Type interfaceType, ICollection<string> namespaceCollector = null, bool isDraftDefault = true )
        {
            if(interfaceType == null) throw new ArgumentNullException( nameof( interfaceType ) );

            _loggingChannel.Info( $"Generating feature definition for type {interfaceType.FullName}" );
            var annotation = interfaceType.GetCustomAttribute<SilaFeatureAttribute>();
            var spec = _generatorConfigSource.GetFeatureSpec( interfaceType );

            var version = spec?.Version;
            var category = spec?.Category;
            var originator = spec?.Originator;

            var versionV = version == null ? interfaceType.Assembly.GetName().Version : Version.Parse( version );
            category ??= annotation?.Category;
            AssignDefaultOriginatorAndCategory( interfaceType, versionV, ref originator, ref category );

            var context = new FeatureDefinitionGeneratorContext( NameRegistry, _translationProvider, _ambiguityResolver, spec, interfaceType );
            context.TryLoadDocumentation( interfaceType.Assembly );
            var (identifier, displayName, description) = context.ParseElementData( interfaceType, spec, prefix: "I" );
            var items = context.GenerateFeature( namespaceCollector );

            var feature = new Feature
            {
                Description = description,
                DisplayName = displayName,
                Identifier = identifier,
                Items = items.ToArray(),
                SiLA2Version = "1.0",
                FeatureVersion = versionV.ToString( 2 ),
                MaturityLevel = (annotation?.IsDraft ?? isDraftDefault) ? FeatureMaturityLevel.Draft : FeatureMaturityLevel.Verified,
                Category = category.ToLowerInvariant(),
                Originator = originator.ToLowerInvariant()
            };

            if(Hooks != null)
            {
                foreach(var hook in Hooks)
                {
                    _loggingChannel.Debug( $"Executing hook {hook} after generating feature" );
                    hook?.OnFeatureGenerated( interfaceType, feature );
                }
            }

            return feature;
        }


        private void AssignDefaultOriginatorAndCategory( Type interfaceType, Version version,
            ref string originator,
            ref string category )
        {
            if(originator == null || category == null)
            {
                var ns = interfaceType.FullName.Substring(0,
                    interfaceType.FullName.Length - interfaceType.Name.Length - 1);
                var endIndex = ns.Length;
                var versionSuffix = "v" + version.Major.ToString();
                if (ns.EndsWith(versionSuffix))
                {
                    endIndex -= versionSuffix.Length + 1;
                }

                int categoryStartIndex = CalculateCategoryStartIndex(ref originator, category, ns);

                if (originator == null)
                {
                    _loggingChannel.Debug("Assign originator based on the namespace because no originator has been provided.");
                    if (categoryStartIndex < 2)
                    {
                        throw new InvalidOperationException("The namespace is too short to derive originator and use case from it. Please provide them explicitly or chose a different namespace.");
                    }
                    originator = ns.Substring(0, categoryStartIndex - 1);
                }

                if (category == null)
                {
                    _loggingChannel.Debug("Assign category based on namespace because no category has been provided.");
                    category = ns.Substring(categoryStartIndex, endIndex - categoryStartIndex);
                }
            }

            originator = AsStandardLower( originator );
            category = AsStandardLower( category );
        }

        private static int CalculateCategoryStartIndex(ref string originator, string category, string ns)
        {
            int categoryStartIndex;
            if (category != null)
            {
                categoryStartIndex = ns.IndexOf(category, StringComparison.OrdinalIgnoreCase);
                if (categoryStartIndex == -1) originator = ns;
            }
            else if (originator != null)
            {
                var originatorStartIndex = ns.IndexOf(originator, StringComparison.OrdinalIgnoreCase);
                if (originatorStartIndex == -1)
                {
                    categoryStartIndex = 0;
                }
                else
                {
                    categoryStartIndex = originatorStartIndex + originator.Length + 1;
                }
            }
            else
            {
                categoryStartIndex = ns.LastIndexOf(".", StringComparison.Ordinal) + 1;
            }

            return categoryStartIndex;
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

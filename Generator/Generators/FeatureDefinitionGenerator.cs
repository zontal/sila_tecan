using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using Tecan.MAP.Contracts.Services.Logging;
using Tecan.Sila2.Generator.Contracts;

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
        private readonly ILoggingChannel _loggingChannel;
        private readonly IAmbiguityResolver _ambiguityResolver;
        /// <summary>
        /// Creates a new feature definition generator for the provided registry
        /// </summary>
        /// <param name="nameRegistry">A component that accepts rename reports</param>
        /// <param name="translationProvider">A component that translates types between SiLA2 and .NET</param>
        /// <param name="loggingChannelFactory">A factory for logging channels</param>
        /// <param name="hooks">Generator hooks</param>
        /// <param name="ambiguityResolver">A component that resolves ambiguous properties</param>
        [ImportingConstructor]
        public FeatureDefinitionGenerator( ICodeNameRegistry nameRegistry, ITypeTranslationProvider translationProvider, ILoggingChannelFactory loggingChannelFactory, 
            [ImportMany] IEnumerable<IGeneratorHook> hooks , IAmbiguityResolver ambiguityResolver)
        {
            NameRegistry = nameRegistry;
            Hooks = hooks;
            _translationProvider = translationProvider;
            _loggingChannel = loggingChannelFactory.CreateChannel( typeof(FeatureDefinitionGenerator) );
            _ambiguityResolver = ambiguityResolver;
        }

        /// <inheritdoc />
        public Feature GenerateFeature( Type interfaceType, string originator = null, string category = null,
            string version = null, ICollection<string> namespaceCollector = null, PredefinedFeatureGenerationConfigModel featureGeneratorConfig = null, bool isDraftDefault = true )
        {
            if(interfaceType == null) throw new ArgumentNullException( nameof( interfaceType ) );

            _loggingChannel.Log($"Generating feature definition for type {interfaceType.FullName}".ToString, Severity.Info);
            var annotation = interfaceType.GetCustomAttribute<SilaFeatureAttribute>();

            var versionV = version == null ? interfaceType.Assembly.GetName().Version : Version.Parse( version );
            category ??= annotation?.Category;
            AssignDefaultOriginatorAndCategory( interfaceType, versionV, ref originator, ref category );

            Func<string, Constraints> constraintResolver = null;
            if (featureGeneratorConfig?.PredefinedConstraints != null )
            {
                constraintResolver = origin =>
                {
                    if(featureGeneratorConfig.PredefinedConstraints.TryGetValue( origin, out var constraint ) )
                    {
                        return constraint;
                    }
                    else
                    {
                        _loggingChannel.Log($"No constraint for element {origin} found, creating only constraints based on annotations.".ToString, Severity.Warning);
                        return null;
                    }
                };
            }

            var context = new FeatureDefinitionGeneratorContext(_loggingChannel, NameRegistry, constraintResolver, _translationProvider, _ambiguityResolver, featureGeneratorConfig);
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
                MaturityLevel = (annotation?.IsDraft ?? isDraftDefault) ? FeatureMaturityLevel.Draft : FeatureMaturityLevel.Verified,
                Category = category.ToLowerInvariant(),
                Originator = originator.ToLowerInvariant()
            };

            if(Hooks != null)
            {
                foreach(var hook in Hooks)
                {
                    _loggingChannel.Log($"Executing hook {hook} after generating feature".ToString);
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
                var ns = interfaceType.FullName.Substring( 0,
                    interfaceType.FullName.Length - interfaceType.Name.Length - 1 );
                int useCaseStartIndex;
                var endIndex = ns.Length;
                var versionSuffix = "v" + version.Major.ToString();
                if(ns.EndsWith( versionSuffix ))
                {
                    endIndex -= versionSuffix.Length + 1;
                }

                if(category != null)
                {
                    useCaseStartIndex = ns.IndexOf( category, StringComparison.OrdinalIgnoreCase );
                    if(useCaseStartIndex == -1) originator = ns;
                }
                else if(originator != null)
                {
                    var originatorStartIndex = ns.IndexOf( originator, StringComparison.OrdinalIgnoreCase );
                    if(originatorStartIndex == -1)
                    {
                        useCaseStartIndex = 0;
                    }
                    else
                    {
                        useCaseStartIndex = originatorStartIndex + originator.Length + 1;
                    }
                }
                else
                {
                    useCaseStartIndex = ns.LastIndexOf( ".", StringComparison.Ordinal ) + 1;
                }

                if( originator == null )
                {
                    _loggingChannel.Log("Assign originator based on the namespace because no originator has been provided.".ToString);
                    if (useCaseStartIndex < 2)
                    {
                        throw new InvalidOperationException( "The namespace is too short to derive originator and use case from it. Please provide them explicitly or chose a different namespace." );
                    }
                    originator = ns.Substring( 0, useCaseStartIndex - 1 );
                }

                if( category == null )
                {
                    _loggingChannel.Log("Assign category based on namespace because no category has been provided.".ToString);
                    category = ns.Substring( useCaseStartIndex, endIndex - useCaseStartIndex );
                }
            }

            originator = AsStandardLower( originator );
            category = AsStandardLower( category );
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

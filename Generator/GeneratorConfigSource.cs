using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator
{
    /// <summary>
    /// Denotes the default config source
    /// </summary>
    [Export( typeof( IGeneratorConfigSource ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    public class GeneratorConfigSource : IGeneratorConfigSource
    {
        private readonly List<FeatureGenerationConfig> _configs = new List<FeatureGenerationConfig>();

        /// <inheritdoc />
        public void Add( FeatureGenerationConfig config )
        {
            _configs.Add( config );
        }

        private IEnumerable<FeatureSpec> AllSpecs() => _configs.SelectMany( c => c.Feature ?? Enumerable.Empty<FeatureSpec>() );

        /// <inheritdoc />
        public FeatureSpec GetFeatureSpec( string identifier )
        {
            return AllSpecs().FirstOrDefault( spec => identifier == (spec.Identifier ?? spec.Code?.Substring( 1 )) );
        }

        /// <inheritdoc />
        public FeatureSpec GetFeatureSpec( Type interfaceType )
        {
            return AllSpecs().FirstOrDefault( spec => spec.Code == interfaceType.Name || spec.Code == interfaceType.FullName );
        }
    }
}

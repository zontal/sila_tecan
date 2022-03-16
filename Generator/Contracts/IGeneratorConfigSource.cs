using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes a component that is the source for code generation specifications
    /// </summary>
    public interface IGeneratorConfigSource
    {
        /// <summary>
        /// Gets the generator specification for the feature with the given identifier
        /// </summary>
        /// <param name="identifier">The feature identifier</param>
        /// <returns>The feature specification</returns>
        FeatureSpec GetFeatureSpec( string identifier );

        /// <summary>
        /// Gets the generator specification for the feature generated for the given interface
        /// </summary>
        /// <param name="interfaceType">The feature interface</param>
        /// <returns>The feature specification</returns>
        FeatureSpec GetFeatureSpec( Type interfaceType );

        /// <summary>
        /// Adds the given configuration into account
        /// </summary>
        /// <param name="config">A deserialized configuration</param>
        void Add( FeatureGenerationConfig config );
    }
}

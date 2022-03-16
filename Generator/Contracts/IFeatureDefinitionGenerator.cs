using System;
using System.Collections.Generic;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Generates features from a given interface
    /// </summary>
    public interface IFeatureDefinitionGenerator
    {
        /// <summary>
        /// Generates a feature for the given interface type
        /// </summary>
        /// <param name="interfaceType">The interface type</param>
        /// <param name="namespaceCollector">A component to which accesses to namespace are persisted</param>
        /// <param name="isDraftDefault">A default value whether a feature is draft</param>
        /// <returns>The generated feature</returns>
        Feature GenerateFeature( Type interfaceType,
            ICollection<string> namespaceCollector = null,
            bool isDraftDefault = true );
    }
}
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
        /// <param name="originator">The originator for the generated feature. If null, an originator is inferred from the namespace</param>
        /// <param name="category">The use case for the generated feature. If null, a use case is inferred from the namespace</param>
        /// <param name="version">The version of the feature</param>
        /// <param name="namespaceCollector">A component to which accesses to namespace are persisted</param>
        /// <param name="constraints">A dictionary of predefined constraints, keyed by fully origin identifiers</param>
        /// <returns>The generated feature</returns>
        Feature GenerateFeature(Type interfaceType,
            string originator = null,
            string category = null,
            string version = null,
            ICollection<string> namespaceCollector = null,
            IReadOnlyDictionary<string, Constraints> constraints = null);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes the class for predefined constrains and ambiguous properties
    /// </summary>
    public class PredefinedFeatureGenerationConfigModel
    {
        /// <summary>
        /// Creates a new  predefined feature generation config model
        /// </summary>
        /// <param name="predefinedConstraints">A dictionary of predefined constraints</param>
        /// <param name="predefinedAmbiguousProperties">A dictionary of predefined ambiguous properties</param>
        public PredefinedFeatureGenerationConfigModel(IReadOnlyDictionary<string, Constraints> predefinedConstraints,
            IDictionary<string, string> predefinedAmbiguousProperties)
        {
            PredefinedConstraints = predefinedConstraints;
            PredefinedAmbiguousProperties = predefinedAmbiguousProperties;
        }
        /// <summary>
        /// A dictionary of predefined constraints, keyed by fully origin identifiers
        /// </summary>
        public IReadOnlyDictionary<string, Constraints> PredefinedConstraints { get; }

        /// <summary>
        /// A dictionary of predefined ambiguous properties, keyed by type name and parameter name
        /// </summary>
        public IDictionary<string, string> PredefinedAmbiguousProperties { get; }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Tecan.Sila2.Generator.Contracts;

namespace Tecan.Sila2.Generator.Helper
{
    /// <summary>
    /// Denotes a helper class to load feature definition config helpers
    /// </summary>
    public static class FeatureDefinitionConfigHelper
    {
        /// <summary>
        /// Loads the feature configuration file
        /// </summary>
        /// <param name="configFile">The absolute path to the config</param>
        /// <returns>The feature definition config</returns>
        public static PredefinedFeatureGenerationConfigModel LoadConfigMappingFile( string configFile )
        {
            if(configFile != null)
            {
                if(File.Exists( configFile ))
                {
                    var serializer = new XmlSerializer( typeof( FeatureGenerationConfig ) );
                    using(var stream = File.OpenRead( configFile ))
                    {
                        var constraints = (FeatureGenerationConfig)serializer.Deserialize( stream );
                        var predefinedConstraints = constraints.Constraint?.ToDictionary( m => m.Element, m => m.Constraint );
                        var predefinedAmbiguousProperties = constraints.Property?.ToDictionary( m => m.Key, m => m.Value );
                        return new PredefinedFeatureGenerationConfigModel( predefinedConstraints, predefinedAmbiguousProperties );
                    }
                }
                else
                {
                    return null;

                }
            }
            return null;
        }
    }
}

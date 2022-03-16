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
        public static FeatureGenerationConfig LoadConfigMappingFile( string configFile )
        {
            if(configFile != null)
            {
                if(File.Exists( configFile ))
                {
                    var serializer = new XmlSerializer( typeof( FeatureGenerationConfig ) );
                    using(var stream = File.OpenRead( configFile ))
                    {
                        var config = (FeatureGenerationConfig)serializer.Deserialize( stream );
                        config.Feature ??= Array.Empty<FeatureSpec>();
                        return config;
                    }
                }
                else
                {
                    return null;

                }
            }
            return null;
        }

        /// <summary>
        /// Adds a specification to apply or override the provided originator, category and version
        /// </summary>
        /// <param name="featureType">The feature type</param>
        /// <param name="config">The config source</param>
        /// <param name="originator">The originator</param>
        /// <param name="category">The category</param>
        /// <param name="version">The version</param>
        public static void AssignStandardValues( Type featureType, IGeneratorConfigSource config, string originator, string category, string version )
        {
            var spec = config.GetFeatureSpec( featureType );
            if(spec == null)
            {
                spec = new FeatureSpec
                {
                    Code = featureType.FullName
                };
                config.Add( new FeatureGenerationConfig { Feature = new FeatureSpec[] { spec } } );
            }
            spec.Originator = originator ?? spec.Originator;
            spec.Category = category ?? spec.Category;
            spec.Version = version ?? spec.Version;
        }
    }
}

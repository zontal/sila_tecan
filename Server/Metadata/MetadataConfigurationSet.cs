using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes the configuration of metadata
    /// </summary>
    [XmlRoot]
    public class MetadataConfigurationSet
    {
        /// <summary>
        /// Gets or sets the availavble metadata configurations
        /// </summary>
        [XmlArray]
        public List<MetadataConfiguration> Configurations
        {
            get;
        } = new List<MetadataConfiguration>();
    }
}

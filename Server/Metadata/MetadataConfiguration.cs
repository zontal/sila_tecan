using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes a configuration of metadata
    /// </summary>
    public class MetadataConfiguration
    {
        /// <summary>
        /// The identifier of the metadata configuration
        /// </summary>
        [XmlAttribute]
        public string Identifier { get; set; }

        /// <summary>
        /// Gets or sets the features, properties or commands affected by this feature
        /// </summary>
        [XmlArray]
        public string[] Affected { get; set; }
    }
}

using System;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Denotes that the property is a client metadata and provides the metadata type
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MetadataTypeAttribute : Attribute
    {
        /// <summary>
        /// The type of the metadata
        /// </summary>
        public Type MetadataType { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="metadataType">The type of the metadata</param>
        public MetadataTypeAttribute(Type metadataType)
        {
            MetadataType = metadataType;
        }
    }
}

using System;

namespace Tecan.Sila2.Server
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MetadataTypeAttribute : Attribute
    {
        public Type MetadataType { get; }

        public MetadataTypeAttribute(Type metadataType)
        {
            MetadataType = metadataType;
        }
    }
}

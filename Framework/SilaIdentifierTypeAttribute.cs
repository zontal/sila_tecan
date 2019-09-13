using System;

namespace Tecan.Sila2
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SilaIdentifierTypeAttribute : Attribute
    {
        public IdentifierType Type { get; }

        public SilaIdentifierTypeAttribute(IdentifierType type)
        {
            Type = type;
        }
    }
}

using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Denotes that the value of a property or parameter should be an identifier
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SilaIdentifierTypeAttribute : Attribute
    {
        /// <summary>
        /// Gets the type of identifier
        /// </summary>
        public IdentifierType Type { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="type">the type of identifier</param>
        public SilaIdentifierTypeAttribute(IdentifierType type)
        {
            Type = type;
        }
    }
}

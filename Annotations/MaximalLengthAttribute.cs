using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates a property or type with the maximal allowed string length
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class MaximalLengthAttribute : Attribute
    {
        /// <summary>
        /// The maximal allowed length of the string
        /// </summary>
        public int MaxLength { get; }

        /// <summary>
        /// Creates a new declaration that the string length of the annotated property must be at most the given value
        /// </summary>
        /// <param name="maxLength"></param>
        public MaximalLengthAttribute(int maxLength)
        {
            MaxLength = maxLength;
        }
    }
}

using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates a property or type with the maximal (inclusive) allowed value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Class)]
    public class MaximalInclusiveAttribute : Attribute
    {
        /// <summary>
        /// Creates a new maximum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MaximalInclusiveAttribute(double threshold)
        {
            Threshold = threshold;
        }

        /// <summary>
        /// The maximum inclusive allowed value
        /// </summary>
        public double Threshold { get; }
    }
}

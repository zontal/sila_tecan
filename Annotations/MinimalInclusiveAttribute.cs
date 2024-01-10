using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates a property or type with the minimum (inclusive) allowed value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Class)]
    public class MinimalInclusiveAttribute : Attribute
    {
        /// <summary>
        /// Creates a new minimum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MinimalInclusiveAttribute(double threshold)
        {
            Threshold = threshold;
        }

        /// <summary>
        /// The minimum inclusive allowed value
        /// </summary>
        public double Threshold { get; }
    }
}

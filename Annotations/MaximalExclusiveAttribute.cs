using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates a property or type with the maximal (exclusive) allowed value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Class)]
    public class MaximalExclusiveAttribute : Attribute
    {
        /// <summary>
        /// Creates a new maximum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MaximalExclusiveAttribute(double threshold)
        {
            Threshold = threshold;
        }

        /// <summary>
        /// The maximum exclusive allowed value
        /// </summary>
        public double Threshold { get; }
    }
}

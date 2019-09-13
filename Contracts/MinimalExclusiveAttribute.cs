using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates a property or type with the minimal (exclusive) allowed value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class MinimalExclusiveAttribute : Attribute
    {
        /// <summary>
        /// Creates a new minimum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MinimalExclusiveAttribute(double threshold)
        {
            Threshold = threshold;
        }

        /// <summary>
        /// The minimum exclusive allowed value
        /// </summary>
        public double Threshold { get; }
    }
}

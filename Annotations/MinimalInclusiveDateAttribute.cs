using System;
using System.Globalization;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates a property or type with the minimum (inclusive) allowed value
    /// </summary>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Class)]
    public class MinimalInclusiveDateAttribute : Attribute
    {
        /// <summary>
        /// Creates a new minimum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MinimalInclusiveDateAttribute( DateTimeOffset threshold )
        {
            Threshold = threshold;
        }

        /// <summary>
        /// Creates a new minimum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MinimalInclusiveDateAttribute( string threshold ) : this( DateTimeOffset.Parse( threshold, CultureInfo.InvariantCulture ) )
        {
        }

        /// <summary>
        /// The minimum inclusive allowed value
        /// </summary>
        public DateTimeOffset Threshold { get; }
    }
}

using System;
using System.Globalization;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates a property or type with the maximal (inclusive) allowed value
    /// </summary>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue )]
    public class MaximalInclusiveDateAttribute : Attribute
    {
        /// <summary>
        /// Creates a new maximum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MaximalInclusiveDateAttribute( DateTimeOffset threshold )
        {
            Threshold = threshold;
        }

        /// <summary>
        /// Creates a new maximum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MaximalInclusiveDateAttribute( string threshold ) : this( DateTimeOffset.Parse( threshold, CultureInfo.InvariantCulture ) )
        {
        }

        /// <summary>
        /// The maximum inclusive allowed value
        /// </summary>
        public DateTimeOffset Threshold { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates a property or type with the maximal (exclusive) allowed value
    /// </summary>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue )]
    public class MaximalExclusiveDateAttribute : Attribute
    {
        /// <summary>
        /// Creates a new maximum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MaximalExclusiveDateAttribute( DateTimeOffset threshold )
        {
            Threshold = threshold;
        }

        /// <summary>
        /// Creates a new maximum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MaximalExclusiveDateAttribute( string threshold ) : this( DateTimeOffset.Parse( threshold, CultureInfo.InvariantCulture ) )
        {
        }

        /// <summary>
        /// The maximum exclusive allowed value
        /// </summary>
        public DateTimeOffset Threshold { get; }
    }
}

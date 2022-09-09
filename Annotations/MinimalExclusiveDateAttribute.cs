using System;
using System.Globalization;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates a property or type with the minimal (exclusive) allowed value
    /// </summary>
    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue )]
    public class MinimalExclusiveDateAttribute : Attribute
    {
        /// <summary>
        /// Creates a new minimum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MinimalExclusiveDateAttribute( DateTimeOffset threshold )
        {
            Threshold = threshold;
        }

        /// <summary>
        /// Creates a new minimum with the given threshold
        /// </summary>
        /// <param name="threshold">The threshold value</param>
        public MinimalExclusiveDateAttribute( string threshold ) : this( DateTimeOffset.Parse( threshold, CultureInfo.InvariantCulture ) )
        {
        }

        /// <summary>
        /// The minimum exclusive allowed value
        /// </summary>
        public DateTimeOffset Threshold { get; }
    }
}

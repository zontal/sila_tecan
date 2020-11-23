using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates a property or type with the constraint that the value must follow a specific pattern
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class PatternConstraintAttribute : Attribute
    {
        /// <summary>
        /// The regular expression that must be followed
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Creates a new annotation that a pattern must be followed
        /// </summary>
        /// <param name="pattern">A .NET regular expression</param>
        public PatternConstraintAttribute(string pattern)
        {
            Pattern = pattern;
        }
    }
}

using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Describes that a property is dynamic
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class ObservableAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the DynamicAttribute class
        /// </summary>
        public ObservableAttribute() { }
    }
}

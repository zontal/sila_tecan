using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Describes that a property is dynamic
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DynamicAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the DynamicAttribute class
        /// </summary>
        public DynamicAttribute() { }

        /// <summary>
        /// Creates a new instance of the DynamicAttribute class
        /// </summary>
        /// <param name="pollingDelay">The time in milliseconds that should be waited between two polls of this property</param>
        public DynamicAttribute(int pollingDelay)
        {
            PollingDelay = pollingDelay;
        }

        /// <summary>
        /// Gets the time that should be waited between two accesses of the annotated property in milliseconds, if specified
        /// </summary>
        public int? PollingDelay { get; set; }
    }
}

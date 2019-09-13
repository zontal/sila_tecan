using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates metadata with a display name used for the SiLA 2 standard.
    /// </summary>
    /// <remarks>If no annotation is specified, the identifier is used as display name</remarks>
    [AttributeUsage(AttributeTargets.All)]
    public class SilaDisplayNameAttribute : Attribute
    {
        /// <summary>
        /// The display name
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Creates a new annotation
        /// </summary>
        /// <param name="displayName">The display name</param>
        public SilaDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}

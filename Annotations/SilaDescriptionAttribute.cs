using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates metadata with a description used for the SiLA 2 standard
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class SilaDescriptionAttribute : Attribute
    {
        /// <summary>
        /// The annotated description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Creates a new annotation
        /// </summary>
        /// <param name="description">The annotated description</param>
        public SilaDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}

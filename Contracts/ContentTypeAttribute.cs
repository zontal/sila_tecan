using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Restricts the content type of the given property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ContentTypeAttribute : Attribute
    {
        /// <summary>
        /// The type of the content
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// The subtype of the content
        /// </summary>
        public string SubType { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="type">The type of the content</param>
        /// <param name="subType">The subtype of the content</param>
        public ContentTypeAttribute(string type, string subType)
        {
            Type = type;
            SubType = subType;
        }
    }
}

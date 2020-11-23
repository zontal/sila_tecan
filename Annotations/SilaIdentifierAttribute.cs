using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Annotates metadata with an identifier used for the SiLA 2 standard
    /// </summary>
    /// <remarks>If no annotation is specified, the member name is used as identifier</remarks>
    [AttributeUsage(AttributeTargets.All)]
    public class SilaIdentifierAttribute : Attribute
    {
        /// <summary>
        /// Gets the identifier
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Creates a new annotation using the given identifier
        /// </summary>
        /// <param name="identifier">The identifier</param>
        public SilaIdentifierAttribute(string identifier)
        {
            Identifier = identifier;
        }
    }
}

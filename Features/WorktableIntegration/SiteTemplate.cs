using System.Collections.Generic;
using System.Linq;

namespace Tecan.Sila2.WorktableIntegration
{
    /// <summary>
    /// Defines a template for a site
    /// </summary>
    public class SiteTemplate
    {
        /// <summary>
        /// Creates a new template
        /// </summary>
        /// <param name="name">The name of the template</param>
        /// <param name="siteRectangle">The size of the site</param>
        /// <param name="tags">A collection of custom tags</param>
        public SiteTemplate(string name, Rectangle siteRectangle, ICollection<string> tags)
        {
            Name = name;
            SiteRectangle = siteRectangle;
            Tags = tags.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the name of the site template
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the size of the site
        /// </summary>
        public Rectangle SiteRectangle { get; }

        /// <summary>
        /// Gets a collection of custom tags
        /// </summary>
        public ICollection<string> Tags { get; }
    }
}

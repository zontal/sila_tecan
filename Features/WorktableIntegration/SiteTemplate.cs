using System.Collections.Generic;
using System.Linq;

namespace Tecan.Sila2.WorktableIntegration
{
    public class SiteTemplate
    {
        public SiteTemplate(string name, Rectangle siteRectangle, ICollection<string> tags)
        {
            Name = name;
            SiteRectangle = siteRectangle;
            Tags = tags.ToList().AsReadOnly();
        }

        public string Name { get; }

        public Rectangle SiteRectangle { get; }

        public ICollection<string> Tags { get; }
    }
}

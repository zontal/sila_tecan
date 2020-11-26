using System;

namespace Tecan.Sila2.WorktableIntegration
{
    /// <summary>
    /// Denotes a site on the worktable
    /// </summary>
    public class Site
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="name">The name of the site</param>
        /// <param name="template">The site template</param>
        /// <param name="offset">The offset from the device</param>
        /// <param name="rotation">The rotation</param>
        public Site(string name, string template, Offset offset, double rotation)
        {
            Name = name;
            Template = template;
            Offset = offset;
            Rotation = rotation;
        }

        /// <summary>
        /// The name of the site
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The site template
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// The offset from the device
        /// </summary>
        public Offset Offset { get; }

        /// <summary>
        /// The rotation
        /// </summary>
        [Unit("Degree", Factor = 180 / Math.PI)]
        public double Rotation { get; }
    }
}

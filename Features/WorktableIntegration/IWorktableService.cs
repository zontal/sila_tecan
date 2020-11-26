using System.Collections.Generic;

namespace Tecan.Sila2.WorktableIntegration
{
    /// <summary>
    /// Denotes a service to describe dimensions of a device in order to integrate it into a worktable
    /// </summary>
    [SilaFeature(true, "Infrastructure")]
    public interface IWorktableService
    {
        /// <summary>
        /// Gets the bounding box of the device
        /// </summary>
        [SilaDescription("The outer hull of the device")]
        BoundingBox BoundingBox { get; }

        /// <summary>
        /// Gets used site templates
        /// </summary>
        [SilaDescription("The templates for sites used by this device")]
        ICollection<SiteTemplate> SiteTemplates { get; }

        /// <summary>
        /// Gets the sites contained in the device
        /// </summary>
        [SilaDescription("The sites that this device offers")]
        ICollection<Site> Sites { get; }
    }
}

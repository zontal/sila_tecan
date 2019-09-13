using System.Collections.Generic;
using Tecan.Sila2;

namespace Tecan.Sila2.WorktableIntegration
{
    [SilaFeature(true, "Infrastructure")]
    public interface IWorktableService
    {
        [SilaDescription("The outer hull of the device")]
        BoundingBox BoundingBox { get; }

        [SilaDescription("The templates for sites used by this device")]
        ICollection<SiteTemplate> SiteTemplates { get; }

        [SilaDescription("The sites that this device offers")]
        ICollection<Site> Sites { get; }
    }
}

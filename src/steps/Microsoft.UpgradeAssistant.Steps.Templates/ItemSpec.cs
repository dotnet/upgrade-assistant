using System.Collections.Generic;
using Microsoft.UpgradeAssistant;

namespace AspNetMigrator.TemplateUpdater
{
    /// <summary>
    /// An item to be added to a project.
    /// </summary>
    public record ItemSpec(ProjectItemType Type, string Path, IEnumerable<string> Keywords);
}

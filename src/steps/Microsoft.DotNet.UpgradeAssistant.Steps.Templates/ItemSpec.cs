using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    /// <summary>
    /// An item to be added to a project.
    /// </summary>
    public record ItemSpec(ProjectItemType Type, string Path, IEnumerable<string> Keywords);
}

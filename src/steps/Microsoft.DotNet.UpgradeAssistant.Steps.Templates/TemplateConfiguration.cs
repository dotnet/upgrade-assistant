using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    public class TemplateConfiguration
    {
        public Dictionary<string, string>? Replacements { get; set; }

        public ItemSpec[]? TemplateItems { get; set; }

        public bool UpdateWebAppsOnly { get; set; }
    }
}

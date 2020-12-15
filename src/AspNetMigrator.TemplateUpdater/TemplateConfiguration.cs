using System.Collections.Generic;

namespace AspNetMigrator.TemplateUpdater
{
    public class TemplateConfiguration
    {
        public Dictionary<string, string>? Replacements { get; set; }

        public ItemSpec[]? TemplateItems { get; set; }

        public bool UpdateWebAppsOnly { get; set; }
    }
}

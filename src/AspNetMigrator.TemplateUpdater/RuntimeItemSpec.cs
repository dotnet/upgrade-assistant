using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace AspNetMigrator.TemplateUpdater
{
    /// <summary>
    /// Internal type that supplements an ItemSpec with replacements relevant to its template configuration
    /// and the relative path to the template file corresponding to the item.
    /// </summary>
    internal record RuntimeItemSpec : ItemSpec
    {
        public RuntimeItemSpec(ItemSpec baseItem, string templateFilePath, Dictionary<string, string> replacements)
            : base(baseItem.Type, baseItem.Path, baseItem.IncludeExplicitly, baseItem.Keywords.ToArray())
        {
            TemplateFilePath = templateFilePath;
            Replacements = ImmutableDictionary.CreateRange(replacements);
        }

        /// <summary>
        /// Gets a dictionary mapping text in the template file
        /// with text that should replace it.
        /// </summary>
        public ImmutableDictionary<string, string>? Replacements { get; }

        /// <summary>
        /// Gets the relative path to the template file.
        /// </summary>
        public string TemplateFilePath { get; }
    }
}

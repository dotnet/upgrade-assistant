using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    /// <summary>
    /// Internal type that supplements an ItemSpec with replacements relevant to its template configuration,
    /// the extension an item comes from, and the relative path to the template file corresponding to the item.
    /// </summary>
    internal record RuntimeItemSpec : ItemSpec
    {
        public RuntimeItemSpec(ItemSpec baseItem, IExtension extension, string templateFilePath, Dictionary<string, string> replacements)
            : base(baseItem.Type, baseItem.Path, baseItem.Keywords.ToArray())
        {
            Extension = extension;
            Replacements = ImmutableDictionary.CreateRange(replacements);
            TemplateFilePath = templateFilePath;
        }

        /// <summary>
        /// Gets a dictionary mapping text in the template file
        /// with text that should replace it.
        /// </summary>
        public ImmutableDictionary<string, string>? Replacements { get; }

        /// <summary>
        /// Gets the relative path of the template file within the extension.
        /// </summary>
        public string TemplateFilePath { get; }

        /// <summary>
        /// Gets the extension the item comes from.
        /// </summary>
        public IExtension Extension { get; }
    }
}

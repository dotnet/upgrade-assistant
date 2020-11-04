using System.Collections.Immutable;

namespace AspNetMigrator.StartupUpdater
{
    /// <summary>
    /// Specification of a project Item that should be included in ASP.NET Core projects.
    /// </summary>
    /// <Remarks>
    /// This is used both for determining whether a project is missing any expected items
    /// and for defining how the items are added if they're missing.
    /// </Remarks>
    public class ItemSpec
    {
        public ItemSpec(string itemType, string itemName, bool includeExplicitly, string[] keywords)
        {
            ItemType = itemType;
            ItemName = itemName;
            IncludeExplicitly = includeExplicitly;
            Keywords = ImmutableArray.Create(keywords);
        }

        /// <summary>
        /// Gets the item's include value.
        /// </summary>
        public string ItemName { get; }

        /// <summary>
        /// Gets the item's type.
        /// </summary>
        public string ItemType { get; }

        /// <summary>
        /// Gets text that is expected to be in the file specified by the item.
        /// </summary>
        public ImmutableArray<string> Keywords { get; }

        /// <summary>
        /// Gets a value indicating whether the item should be added with an explicit include
        /// statement in the project or if it will be included automatically.
        /// </summary>
        public bool IncludeExplicitly { get; }
    }
}

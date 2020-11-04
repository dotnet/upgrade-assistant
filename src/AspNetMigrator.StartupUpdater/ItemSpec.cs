using System.Collections.Immutable;

namespace AspNetMigrator.StartupUpdater
{
    public class ItemSpec
    {
        public ItemSpec(string itemType, string itemName, string[] keywords)
        {
            ItemType = itemType;
            ItemName = itemName;
            Keywords = ImmutableArray.Create(keywords);
        }

        public string ItemName { get; }

        public string ItemType { get; }

        public ImmutableArray<string> Keywords { get; }
    }
}

namespace AspNetMigrator.StartupUpdater
{
    public class ItemSpec
    {
        public ItemSpec(string itemType, string itemName, string[] keywords)
        {
            ItemType = itemType;
            ItemName = itemName;
            Keywords = keywords;
        }

        public string ItemName { get; }
        public string ItemType { get; }
        public string[] Keywords { get; }
    }
}

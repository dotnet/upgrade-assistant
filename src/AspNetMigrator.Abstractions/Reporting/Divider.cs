namespace AspNetMigrator.Reporting
{
    public record Divider : Content
    {
        private Divider()
        {
        }

        internal static Divider Instance { get; } = new Divider();
    }
}

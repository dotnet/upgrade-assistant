namespace Microsoft.DotNet.UpgradeAssistant.Reporting
{
    public record Divider : Content
    {
        private Divider()
        {
        }

        internal static Divider Instance { get; } = new Divider();
    }
}

namespace Microsoft.UpgradeAssistant
{
    public interface ITargetFrameworkMonikerFactory
    {
        TargetFrameworkMoniker GetTFMForNetFxVersion(string netFxVersion);
    }
}

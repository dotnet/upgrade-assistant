namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface ITargetFrameworkMonikerFactory
    {
        TargetFrameworkMoniker GetTFMForNetFxVersion(string netFxVersion);
    }
}

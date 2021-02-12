namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public interface IVisualStudioFinder
    {
        string? GetLatestVisualStudioPath();
    }
}

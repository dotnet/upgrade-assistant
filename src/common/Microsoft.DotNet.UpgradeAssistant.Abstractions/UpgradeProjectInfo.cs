namespace Microsoft.DotNet.UpgradeAssistant
{
    public record UpgradeProjectInfo(IProject Project, ITargetTFMSelector Selector)
    {
        public TargetFrameworkMoniker TargetTFM => Selector.SelectTFM(Project);
    }
}

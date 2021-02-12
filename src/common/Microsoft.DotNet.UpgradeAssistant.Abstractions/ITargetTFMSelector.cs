namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface ITargetTFMSelector
    {
        /// <summary>
        /// Chooses the most likely target TFM a project should be retargeted to based on its style, output type, dependencies, and
        /// the user's preference of current or LTS.
        /// </summary>
        TargetFrameworkMoniker SelectTFM(IProject project);
    }
}

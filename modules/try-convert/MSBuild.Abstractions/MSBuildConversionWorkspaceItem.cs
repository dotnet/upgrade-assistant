namespace MSBuild.Abstractions
{
    public class MSBuildConversionWorkspaceItem
    {
        public IProjectRootElement ProjectRootElement { get; }
        public UnconfiguredProject UnconfiguredProject { get; }
        public BaselineProject SdkBaselineProject { get; }

        public MSBuildConversionWorkspaceItem(IProjectRootElement root, UnconfiguredProject unconfiguredProject, BaselineProject baseline)
        {
            ProjectRootElement = root;
            UnconfiguredProject = unconfiguredProject;
            SdkBaselineProject = baseline;
        }
    }
}

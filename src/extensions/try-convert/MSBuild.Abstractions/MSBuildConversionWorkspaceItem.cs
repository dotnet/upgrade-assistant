// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class WorkspaceOptions
    {
        public string InputPath { get; set; } = null!;

        public string? MSBuildPath { get; set; }

        public string? VisualStudioPath { get; set; }
    }
}

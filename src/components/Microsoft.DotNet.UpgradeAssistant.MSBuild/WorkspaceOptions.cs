// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class WorkspaceOptions
    {
        [Required]
        public string InputPath { get; set; } = null!;

        public string? VisualStudioPath { get; set; }
    }
}

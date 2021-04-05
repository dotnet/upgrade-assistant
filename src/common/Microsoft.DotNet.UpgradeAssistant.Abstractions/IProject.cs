// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IProject
    {
        FileInfo FileInfo { get; }

        Language Language { get; }

        Project GetRoslynProject();

        IEnumerable<IProject> ProjectReferences { get; }

        IEnumerable<Reference> FrameworkReferences { get; }

        ValueTask<INuGetReferences> GetNuGetReferencesAsync(CancellationToken token);

        IEnumerable<Reference> References { get; }

        IReadOnlyCollection<TargetFrameworkMoniker> TargetFrameworks { get; }

        ValueTask<ProjectComponents> GetComponentsAsync(CancellationToken token);

        ProjectOutputType OutputType { get; }

        IEnumerable<string> FindFiles(ProjectItemType itemType, ProjectItemMatcher matcher);

        IProjectFile GetFile();
    }
}

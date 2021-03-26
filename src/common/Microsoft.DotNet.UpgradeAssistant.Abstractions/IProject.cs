// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IProject
    {
        FileInfo FileInfo { get; }

        Language Language { get; }

        Project GetRoslynProject();

        IEnumerable<IProject> ProjectReferences { get; }

        NugetPackageFormat PackageReferenceFormat { get; }

        IEnumerable<Reference> FrameworkReferences { get; }

        IEnumerable<NuGetReference> PackageReferences { get; }

        IEnumerable<NuGetReference> GetTransitivePackageReferences(TargetFrameworkMoniker tfm);

        string? LockFilePath { get; }

        IEnumerable<Reference> References { get; }

        IReadOnlyCollection<TargetFrameworkMoniker> TargetFrameworks { get; }

        ProjectComponents Components { get; }

        ProjectOutputType OutputType { get; }

        IEnumerable<string> FindFiles(ProjectItemType itemType, ProjectItemMatcher matcher);

        IProjectFile GetFile();
    }
}

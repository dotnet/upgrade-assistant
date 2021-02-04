using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator
{
    public interface IProject
    {
        string Directory { get; }

        string FilePath { get; }

        Project GetRoslynProject();

        IEnumerable<IProject> ProjectReferences { get; }

        NugetPackageFormat PackageReferenceFormat { get; }

        IEnumerable<NuGetReference> PackageReferences { get; }

        TargetFrameworkMoniker TFM { get; }

        IEnumerable<string> FindFiles(ProjectItemType itemType, ProjectItemMatcher matcher);

        IProjectFile GetFile();
    }
}

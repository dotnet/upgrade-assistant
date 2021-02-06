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

        IEnumerable<Reference> FrameworkReferences { get; }

        IEnumerable<NuGetReference> PackageReferences { get; }

        IEnumerable<Reference> References { get; }

        TargetFrameworkMoniker TFM { get; }

        ProjectStyle Style { get; }

        ProjectOutputType OutputType { get; }

        IEnumerable<string> FindFiles(ProjectItemType itemType, ProjectItemMatcher matcher);

        IProjectFile GetFile();
    }
}

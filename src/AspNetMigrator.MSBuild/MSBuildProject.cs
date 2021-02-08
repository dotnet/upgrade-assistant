using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

using Build = Microsoft.Build.Evaluation;

namespace AspNetMigrator.MSBuild
{
    internal partial class MSBuildProject : IProject
    {
        private readonly ILogger _logger;

        public MSBuildWorkspaceMigrationContext Context { get; }

        public string FilePath { get; }

        public string Directory => Path.GetDirectoryName(FilePath)!;

        public MSBuildProject(MSBuildWorkspaceMigrationContext context, string path, ILogger logger)
        {
            FilePath = path;
            Context = context;
            _logger = logger;
        }

        public IEnumerable<IProject> ProjectReferences => GetRoslynProject().ProjectReferences.Select(p =>
        {
            var project = Context.Workspace.CurrentSolution.GetProject(p.ProjectId);

            if (project?.FilePath is null)
            {
                throw new InvalidOperationException("Could not find project path for reference");
            }

            return Context.GetOrAddProject(project.FilePath).Project;
        });

        public Build.Project Project => Context.ProjectCollection.LoadProject(FilePath);

        public ProjectOutputType OutputType =>
            ProjectRoot.Properties.FirstOrDefault(p => p.Name.Equals(MSBuildConstants.OutputTypePropertyName, StringComparison.OrdinalIgnoreCase))?.Value switch
            {
                MSBuildConstants.LibraryPropertyValue => ProjectOutputType.Library,
                MSBuildConstants.ExePropertyValue => ProjectOutputType.Exe,
                MSBuildConstants.WinExePropertyValue => ProjectOutputType.WinExe,
                null => GetDefaultOutputType(),
                _ => ProjectOutputType.Other
            };

        private ProjectOutputType GetDefaultOutputType()
        {
            if (IsSdk && MSBuildConstants.SDKsWithExeDefaultOutputType.Contains(Sdk, StringComparer.OrdinalIgnoreCase))
            {
                return ProjectOutputType.Exe;
            }

            return ProjectOutputType.Library;
        }

        public ProjectStyle Style => IsSdk
            ? GetSDKProjectStyle()
            : GetOldProjectStyle();

        // Gets project style based on SDK, properties, and FrameworkReferences
        private ProjectStyle GetSDKProjectStyle()
        {
            if (Sdk.Equals(MSBuildConstants.WebSdk, StringComparison.OrdinalIgnoreCase))
            {
                return ProjectStyle.Web;
            }

            if (Sdk.Equals(MSBuildConstants.DesktopSdk, StringComparison.OrdinalIgnoreCase) ||
                GetPropertyValue("UseWPF").Equals("true", StringComparison.OrdinalIgnoreCase) ||
                GetPropertyValue("UseWindowsForms").Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return ProjectStyle.WindowsDesktop;
            }

            var frameworkReferenceNames = FrameworkReferences.Select(r => r.Name);
            if (frameworkReferenceNames.Any(f => MSBuildConstants.WebFrameworkReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
            {
                return ProjectStyle.Web;
            }

            if (frameworkReferenceNames.Any(f => MSBuildConstants.DesktopFrameworkReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
            {
                return ProjectStyle.WindowsDesktop;
            }

            return ProjectStyle.Default;
        }

        // Gets project style based on imports and References
        private ProjectStyle GetOldProjectStyle()
        {
            // Check imports and references
            var importedProjects = ProjectRoot.Imports.Select(p => Path.GetFileName(p.Project));
            var references = References.Select(r => r.Name);

            if (importedProjects.Contains(MSBuildConstants.WebApplicationTargets, StringComparer.OrdinalIgnoreCase) ||
                references.Any(r => MSBuildConstants.WebReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                return ProjectStyle.Web;
            }

            if (references.Any(r => MSBuildConstants.WinFormsReferences.Contains(r, StringComparer.OrdinalIgnoreCase)) ||
                references.Any(r => MSBuildConstants.WPFReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                return ProjectStyle.WindowsDesktop;
            }

            return ProjectStyle.Default;
        }

        public IEnumerable<string> FindFiles(ProjectItemType itemType, ProjectItemMatcher matcher)
        {
            var items = Project.Items
                .Where<Build.ProjectItem>(i => i.ItemType.Equals(itemType.Name) && matcher.Match(i.EvaluatedInclude));

            foreach (var item in items)
            {
                yield return Path.IsPathFullyQualified(item.EvaluatedInclude)
                    ? item.EvaluatedInclude
                    : Path.Combine(Path.GetDirectoryName(FilePath) ?? string.Empty, item.EvaluatedInclude);
            }
        }

        public NugetPackageFormat PackageReferenceFormat
            => GetPackagesConfigPath() is null ? NugetPackageFormat.PackageReference : NugetPackageFormat.PackageConfig;

        private string? GetPackagesConfigPath() => FindFiles(ProjectItemType.Content, "packages.config").FirstOrDefault();

        public IEnumerable<NuGetReference> PackageReferences
        {
            get
            {
                var packagesConfig = GetPackagesConfigPath();

                if (packagesConfig is null)
                {
                    var packages = ProjectRoot.GetAllPackageReferences();

                    return packages.Select(p => p.AsNuGetReference()).ToList();
                }
                else
                {
                    return PackageConfig.GetPackages(packagesConfig);
                }
            }
        }

        public IEnumerable<Reference> FrameworkReferences =>
            ProjectRoot.GetAllFrameworkReferences().Select(r => r.AsReference()).ToList();

        public IEnumerable<Reference> References =>
            ProjectRoot.GetAllReferences().Select(r => r.AsReference()).ToList();

        public TargetFrameworkMoniker TFM
        {
            get
            {
                if (IsSdk)
                {
                    // Currently only supporting non-multi-targeting scenarios
                    var value = ProjectRoot.Properties
                        .Single(e => e.Name == "TargetFramework")
                        .Value;

                    return new TargetFrameworkMoniker(value);
                }
                else
                {
                    var value = ProjectRoot.Properties
                        .Single(e => e.Name == "TargetFrameworkVersion")
                        .Value;

                    return TargetFrameworkMoniker.ParseNetFxVersion(value);
                }
            }
        }

        IProjectFile IProject.GetFile() => this;

        public override bool Equals(object? obj)
        {
            if (obj is MSBuildProject other)
            {
                return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath);

        public Project GetRoslynProject()
            => Context.Workspace.CurrentSolution.Projects.First(p => string.Equals(p.FilePath, FilePath, StringComparison.OrdinalIgnoreCase));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

using MBuild = Microsoft.Build.Evaluation;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
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

        public MBuild.Project Project => Context.ProjectCollection.LoadProject(FilePath);

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

        public ProjectComponents Components
        {
            get
            {
                var components = IsSdk ? GetSDKProjectComponents() : GetOldProjectComponents();

                if (PackageReferences.Any(f => MSBuildConstants.WinRTPackages.Contains(f.Name, StringComparer.OrdinalIgnoreCase)))
                {
                    components |= ProjectComponents.WinRT;
                }

                return components;

                // Gets project components based on SDK, properties, and FrameworkReferences
                ProjectComponents GetSDKProjectComponents()
                {
                    var components = ProjectComponents.None;
                    if (Sdk.Equals(MSBuildConstants.WebSdk, StringComparison.OrdinalIgnoreCase))
                    {
                        components |= ProjectComponents.Web;
                    }

                    if (Sdk.Equals(MSBuildConstants.DesktopSdk, StringComparison.OrdinalIgnoreCase) ||
                        GetPropertyValue("UseWPF").Equals("true", StringComparison.OrdinalIgnoreCase) ||
                        GetPropertyValue("UseWindowsForms").Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        components |= ProjectComponents.WindowsDesktop;
                    }

                    var frameworkReferenceNames = FrameworkReferences.Select(r => r.Name);
                    if (frameworkReferenceNames.Any(f => MSBuildConstants.WebFrameworkReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
                    {
                        components |= ProjectComponents.Web;
                    }

                    if (frameworkReferenceNames.Any(f => MSBuildConstants.DesktopFrameworkReferences.Contains(f, StringComparer.OrdinalIgnoreCase)))
                    {
                        components |= ProjectComponents.WindowsDesktop;
                    }

                    return components;
                }

                // Gets project components based on imports and References
                ProjectComponents GetOldProjectComponents()
                {
                    var components = ProjectComponents.None;

                    // Check imports and references
                    var importedProjects = ProjectRoot.Imports.Select(p => Path.GetFileName(p.Project));
                    var references = References.Select(r => r.Name);

                    if (importedProjects.Contains(MSBuildConstants.WebApplicationTargets, StringComparer.OrdinalIgnoreCase) ||
                        references.Any(r => MSBuildConstants.WebReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
                    {
                        components |= ProjectComponents.Web;
                    }

                    if (references.Any(r => MSBuildConstants.WinFormsReferences.Contains(r, StringComparer.OrdinalIgnoreCase)) ||
                        references.Any(r => MSBuildConstants.WPFReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
                    {
                        components |= ProjectComponents.WindowsDesktop;
                    }

                    return components;
                }
            }
        }

        public IEnumerable<string> FindFiles(ProjectItemType itemType, ProjectItemMatcher matcher)
        {
            var items = Project.Items
                .Where<MBuild.ProjectItem>(i => i.ItemType.Equals(itemType.Name) && matcher.Match(i.EvaluatedInclude));

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

                    return Context.TfmFactory.GetTFMForNetFxVersion(value);
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

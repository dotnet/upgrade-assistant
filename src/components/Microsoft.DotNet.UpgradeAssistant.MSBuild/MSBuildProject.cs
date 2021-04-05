// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.ProjectModel;

using MBuild = Microsoft.Build.Evaluation;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal partial class MSBuildProject : IProject
    {
        private readonly ILogger _logger;
        private readonly IComponentIdentifier _componentIdentifier;
        private readonly IPackageRestorer _restorer;

        public MSBuildWorkspaceUpgradeContext Context { get; }

        public FileInfo FileInfo { get; }

        public MSBuildProject(
            MSBuildWorkspaceUpgradeContext context,
            IComponentIdentifier componentIdentifier,
            IPackageRestorer restorer,
            FileInfo file,
            ILogger logger)
        {
            FileInfo = file;
            Context = context;

            _componentIdentifier = componentIdentifier;
            _restorer = restorer;
            _logger = logger;
        }

        public IEnumerable<IProject> ProjectReferences => GetRoslynProject().ProjectReferences.Select(p =>
        {
            var project = Context.Workspace.CurrentSolution.GetProject(p.ProjectId);

            if (project?.FilePath is null)
            {
                throw new InvalidOperationException("Could not find project path for reference");
            }

            return Context.GetOrAddProject(new FileInfo(project.FilePath));
        });

        public Language Language => ParseLanguageByProjectFileExtension(FileInfo.Extension);

        private static Language ParseLanguageByProjectFileExtension(string extension)
            => extension.ToUpperInvariant() switch
            {
                ".CSPROJ" => Language.CSharp,
                ".VBPROJ" => Language.VisualBasic,
                ".FSPROJ" => Language.FSharp,
                _ => Language.Unknown
            };

        public MBuild.Project Project => Context.ProjectCollection.LoadProject(FileInfo.FullName);

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

        public ValueTask<ProjectComponents> GetComponentsAsync(CancellationToken token)
            => _componentIdentifier.GetComponents(this, token);

        public IEnumerable<string> FindFiles(ProjectItemType itemType, ProjectItemMatcher matcher)
        {
            var items = Project.Items
                .Where<MBuild.ProjectItem>(i => i.ItemType.Equals(itemType.Name, StringComparison.Ordinal) && matcher.Match(i.EvaluatedInclude));

            foreach (var item in items)
            {
                yield return Path.IsPathFullyQualified(item.EvaluatedInclude)
                    ? item.EvaluatedInclude
                    : Path.Combine(FileInfo.DirectoryName ?? string.Empty, item.EvaluatedInclude);
            }
        }

        public NugetPackageFormat PackageReferenceFormat
        {
            get
            {
                if (GetPackagesConfigPath() is not null)
                {
                    return NugetPackageFormat.PackageConfig;
                }
                else if (ProjectRoot.GetAllPackageReferences().ToList() is IEnumerable<Build.Construction.ProjectItemElement> list && list.Any())
                {
                    return NugetPackageFormat.PackageReference;
                }
                else
                {
                    return NugetPackageFormat.None;
                }
            }
        }

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

        public IEnumerable<NuGetReference> GetTransitivePackageReferences(TargetFrameworkMoniker tfm)
        {
            if (PackageReferenceFormat != NugetPackageFormat.PackageReference)
            {
                return Enumerable.Empty<NuGetReference>();
            }

            var parsedTfm = NuGetFramework.Parse(tfm.Name);
            var lockFile = LockFileUtilities.GetLockFile(LockFilePath, NuGet.Common.NullLogger.Instance);

            if (lockFile is null)
            {
                throw new ProjectRestoreRequiredException($"Project is not restored: {FileInfo}");
            }

            var lockFileTarget = lockFile
                .Targets
                .FirstOrDefault(t => t.TargetFramework.DotNetFrameworkName.Equals(parsedTfm.DotNetFrameworkName, StringComparison.Ordinal));

            if (lockFileTarget is null)
            {
                throw new ProjectRestoreRequiredException($"Could not find {parsedTfm.DotNetFrameworkName} in {LockFilePath} for {FileInfo}");
            }

            return lockFileTarget.Libraries.Select(l => new NuGetReference(l.Name, l.Version.ToNormalizedString()));
        }

        public string? LockFilePath
        {
            get
            {
                var lockFilePath = Path.Combine(GetPropertyValue("MSBuildProjectExtensionsPath"), "project.assets.json");

                if (string.IsNullOrEmpty(lockFilePath))
                {
                    return null;
                }

                if (!Path.IsPathFullyQualified(lockFilePath))
                {
                    lockFilePath = Path.Combine(FileInfo.DirectoryName ?? string.Empty, lockFilePath);
                }

                return lockFilePath;
            }
        }

        public IEnumerable<Reference> FrameworkReferences =>
            ProjectRoot.GetAllFrameworkReferences().Select(r => r.AsReference()).ToList();

        public IEnumerable<Reference> References =>
            ProjectRoot.GetAllReferences().Select(r => r.AsReference()).ToList();

        public IReadOnlyCollection<TargetFrameworkMoniker> TargetFrameworks => new TargetFrameworkMonikerCollection(this);

        IProjectFile IProject.GetFile() => this;

        public override bool Equals(object? obj)
        {
            if (obj is MSBuildProject other)
            {
                return string.Equals(FileInfo.FullName, other.FileInfo.FullName, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FileInfo);

        public Project GetRoslynProject()
            => Context.Workspace.CurrentSolution.Projects.First(p => string.Equals(p.FilePath, FileInfo.FullName, StringComparison.OrdinalIgnoreCase));
    }
}

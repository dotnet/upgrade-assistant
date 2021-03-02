// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public MSBuildWorkspaceUpgradeContext Context { get; }

        public string FilePath { get; }

        public string Directory => Path.GetDirectoryName(FilePath)!;

        public MSBuildProject(MSBuildWorkspaceUpgradeContext context, string path, ILogger logger)
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

            return Context.GetOrAddProject(project.FilePath);
        });

        public Languages Language => ParseLanguageByProjectFileExtension(FilePath);

        private static Languages ParseLanguageByProjectFileExtension(string filePath)
        {
            return Path.GetExtension(filePath).ToUpperInvariant() switch
            {
                ".CSPROJ"=> Languages.CSharp,
                ".VBPROJ"=> Languages.VisualBasic,
                ".FSPROJ"=> Languages.FSharp,
                _ => Languages.Unknown
            };
        }

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

                if (TransitivePackageReferences.Any(f => MSBuildConstants.WinRTPackages.Contains(f.Name, StringComparer.OrdinalIgnoreCase)))
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

                    if (GetPropertyValue("UseWPF").Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        components |= ProjectComponents.WPF;
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

                    if (references.Any(r => MSBuildConstants.WPFReferences.Contains(r, StringComparer.OrdinalIgnoreCase)))
                    {
                        components |= ProjectComponents.WPF;
                    }

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
                .Where<MBuild.ProjectItem>(i => i.ItemType.Equals(itemType.Name, StringComparison.Ordinal) && matcher.Match(i.EvaluatedInclude));

            foreach (var item in items)
            {
                yield return Path.IsPathFullyQualified(item.EvaluatedInclude)
                    ? item.EvaluatedInclude
                    : Path.Combine(Path.GetDirectoryName(FilePath) ?? string.Empty, item.EvaluatedInclude);
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

        public IEnumerable<NuGetReference> TransitivePackageReferences
        {
            get
            {
                return PackageReferences.Concat(GetTransitiveDependencies()).Distinct();

                IEnumerable<NuGetReference> GetTransitiveDependencies()
                {
                    if (PackageReferenceFormat != NugetPackageFormat.PackageReference)
                    {
                        return Enumerable.Empty<NuGetReference>();
                    }

                    var tfm = NuGetFramework.Parse(TFM.Name);
                    var lockFile = LockFileUtilities.GetLockFile(LockFilePath, NuGet.Common.NullLogger.Instance);

                    if (lockFile is null)
                    {
                        throw new ProjectRestoreRequiredException($"Project is not restored: {FilePath}");
                    }

                    var lockFileTarget = lockFile
                        .Targets
                        .FirstOrDefault(t => t.TargetFramework.DotNetFrameworkName.Equals(tfm.DotNetFrameworkName, StringComparison.Ordinal));

                    if (lockFileTarget is null)
                    {
                        throw new ProjectRestoreRequiredException($"Could not find {tfm.DotNetFrameworkName} in {LockFilePath} for {FilePath}");
                    }

                    return lockFileTarget.Libraries.Select(l => new NuGetReference(l.Name, l.Version.ToNormalizedString()));
                }
            }
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
                    lockFilePath = Path.Combine(Directory, lockFilePath);
                }

                return lockFilePath;
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
                    var targetFrameworkProperties = ProjectRoot.Properties.Where(e => e.Name.Equals("TargetFramework", StringComparison.Ordinal));
                    var propCount = targetFrameworkProperties.Count();

                    if (propCount != 1)
                    {
                        _logger.LogCritical("SDK projects being upgraded must have exactly one TargetFramework property. Found {TargetFrameworkCount} TargetFramework properties.", propCount);
                        throw new UpgradeException($"SDK projects being upgraded must have exactly one TargetFramework property. Found {propCount} TargetFramework properties.");
                    }

                    return new TargetFrameworkMoniker(targetFrameworkProperties.Single().Value);
                }
                else
                {
                    var targetFrameworkVersionProperties = ProjectRoot.Properties.Where(e => e.Name.Equals("TargetFrameworkVersion", StringComparison.Ordinal));
                    var propCount = targetFrameworkVersionProperties.Count();

                    if (propCount != 1)
                    {
                        _logger.LogCritical("Non-SDK projects being upgraded must have exactly one TargetFrameworkVersion property. Found {TargetFrameworkVersionCount} TargetFrameworkVerion properties.", propCount);
                        throw new UpgradeException($"SDK projects being upgraded must have exactly one TargetFrameworkVersion property. Found {propCount} TargetFrameworkVersion properties.");
                    }

                    return Context.TfmFactory.GetTFMForNetFxVersion(targetFrameworkVersionProperties.Single().Value);
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

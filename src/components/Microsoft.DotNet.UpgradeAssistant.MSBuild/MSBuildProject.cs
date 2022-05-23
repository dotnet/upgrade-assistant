// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

using MBuild = Microsoft.Build.Evaluation;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal partial class MSBuildProject : IProject
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IComponentIdentifier> _componentIdentifiers;
        private readonly Factories _factories;

        public MSBuildWorkspaceUpgradeContext Context { get; }

        public FileInfo FileInfo { get; }

        public MSBuildProject(
            MSBuildWorkspaceUpgradeContext context,
            IEnumerable<IComponentIdentifier> componentIdentifiers,
            Factories factories,
            FileInfo file,
            ILogger<MSBuildProject> logger)
        {
            FileInfo = file ?? throw new ArgumentNullException(nameof(file));
            Context = context ?? throw new ArgumentNullException(nameof(context));

            _factories = factories ?? throw new ArgumentNullException(nameof(factories));
            _componentIdentifiers = componentIdentifiers ?? throw new ArgumentNullException(nameof(componentIdentifiers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Id => Context.SolutionInfo.GetProjectId(FileInfo.FullName);

        public IEnumerable<IProject> ProjectReferences => GetRoslynProject().ProjectReferences.Select(p =>
        {
            var project = Context.Workspace.CurrentSolution.GetProject(p.ProjectId);

            if (project?.FilePath is null)
            {
                throw new InvalidOperationException("Could not find project path for reference");
            }

            return Context.GetProject(project.FilePath);
        });

        public Language Language => ParseLanguageByProjectFileExtension(FileInfo.Extension);

        IEnumerable<NuGetReference> IProject.PackageReferences
        {
            get
            {
                var items = Project.GetItems(MSBuildConstants.PackageReferenceType);

                if (items is null)
                {
                    return Enumerable.Empty<NuGetReference>();
                }

                return items.Select(i => i.AsNuGetReference());
            }
        }

        public INuGetReferences NuGetReferences => _factories.CreateNuGetReferences(Context, this);

        private static Language ParseLanguageByProjectFileExtension(string extension)
            => extension.ToUpperInvariant() switch
            {
                ".CSPROJ" => Language.CSharp,
                ".VBPROJ" => Language.VisualBasic,
                ".FSPROJ" => Language.FSharp,
                _ => Language.Unknown
            };

        public MBuild.Project Project
        {
            get
            {
                try
                {
                    return Context.ProjectCollection.LoadProject(FileInfo.FullName);
                }
                catch (InvalidProjectFileException)
                {
                    // TODO Delete obj file retry - a change in platform version may cause load to fail for UWP/WinUI apps
                    var fileDirectory = FileInfo.Directory;
                    var dirToDelete = fileDirectory?.EnumerateDirectories().Where(directory => directory.Name == "obj").FirstOrDefault();
                    if (dirToDelete != null)
                    {
                        dirToDelete.Delete(true);
                    }

                    try
                    {
                        return Context.ProjectCollection.LoadProject(FileInfo.FullName);
                    }
                    catch (InvalidProjectFileException e)
                    {
                        throw new UpgradeException(LocalizedStrings.InvalidProjectError, e);
                    }
                }
            }
        }

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
            _logger.LogDebug("Could not find an output type");

            if (Sdk.Any(p => MSBuildConstants.SDKsWithExeDefaultOutputType.Contains(p, StringComparer.OrdinalIgnoreCase)))
            {
                return ProjectOutputType.Exe;
            }

            return ProjectOutputType.Library;
        }

        public async ValueTask<ProjectComponents> GetComponentsAsync(CancellationToken token)
        {
            var component = ProjectComponents.None;

            foreach (var identifier in _componentIdentifiers)
            {
                component |= await identifier.GetComponentsAsync(this, token).ConfigureAwait(false);
            }

            return component;
        }

        public IEnumerable<string> FindFiles(ProjectItemMatcher matcher, ProjectItemType? itemType = null)
        {
            var items = Project.Items
                .Where<MBuild.ProjectItem>(i => matcher.Match(i.EvaluatedInclude));

            if (itemType is not null)
            {
                items = items.Where(i => i.ItemType.Equals(itemType.Name, StringComparison.Ordinal));
            }

            foreach (var item in items)
            {
                yield return Path.IsPathFullyQualified(item.EvaluatedInclude)
                    ? item.EvaluatedInclude
                    : Path.Combine(FileInfo.DirectoryName ?? string.Empty, item.EvaluatedInclude);
            }
        }

        public IEnumerable<Reference> FrameworkReferences =>
            ProjectRoot.GetAllFrameworkReferences().Select(r => r.AsReference()).ToList();

        public IEnumerable<Reference> References =>
            ProjectRoot.GetAllReferences().Select(r => r.AsReference()).ToList();

        public IReadOnlyCollection<TargetFrameworkMoniker> TargetFrameworks => _factories.CreateTfmCollection(this);

        public IEnumerable<string> ProjectTypes => GetPropertyValue("ProjectTypeGuids").Split(';');

        public override bool Equals(object? obj)
        {
            if (obj is MSBuildProject other)
            {
                return string.Equals(FileInfo.FullName, other.FileInfo.FullName, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FileInfo.FullName);

        public Project GetRoslynProject()
            => Context.Workspace.CurrentSolution.Projects.First(p => string.Equals(p.FilePath, FileInfo.FullName, StringComparison.OrdinalIgnoreCase));

        public override string ToString() => FileInfo.Name;
    }
}

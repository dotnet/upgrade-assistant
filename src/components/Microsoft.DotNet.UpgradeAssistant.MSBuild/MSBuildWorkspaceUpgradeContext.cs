// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal sealed class MSBuildWorkspaceUpgradeContext : IUpgradeContext, IDisposable
    {
        private readonly IPackageRestorer _restorer;
        private readonly ITargetFrameworkMonikerComparer _comparer;
        private readonly IComponentIdentifier _componentIdentifier;
        private readonly ILogger<MSBuildWorkspaceUpgradeContext> _logger;
        private readonly string? _vsPath;
        private readonly Dictionary<string, IProject> _projectCache;

        private List<FileInfo>? _entryPointPaths;
        private FileInfo? _projectPath;

        private MSBuildWorkspace? _workspace;

        public string InputPath { get; }

        public bool InputIsSolution => InputPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase);

        public MSBuildWorkspace Workspace
        {
            get
            {
                if (_workspace is null)
                {
                    throw new InvalidOperationException("No workspace available.");
                }

                return _workspace;
            }
        }

        public bool IsComplete { get; set; }

        public MSBuildWorkspaceUpgradeContext(
            UpgradeOptions options,
            IVisualStudioFinder vsFinder,
            IPackageRestorer restorer,
            ITargetFrameworkMonikerComparer comparer,
            IComponentIdentifier componentIdentifier,
            ILogger<MSBuildWorkspaceUpgradeContext> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (vsFinder is null)
            {
                throw new ArgumentNullException(nameof(vsFinder));
            }

            _projectCache = new Dictionary<string, IProject>(StringComparer.OrdinalIgnoreCase);
            InputPath = options.ProjectPath;
            _restorer = restorer;
            _comparer = comparer;
            _componentIdentifier = componentIdentifier ?? throw new ArgumentNullException(nameof(componentIdentifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vsPath = vsFinder.GetLatestVisualStudioPath();

            GlobalProperties = CreateProperties();
            ProjectCollection = new ProjectCollection(globalProperties: GlobalProperties);
        }

        public ProjectCollection ProjectCollection { get; }

        public void Dispose()
        {
            _workspace?.Dispose();
            _workspace = null;
            ProjectCollection.Dispose();
        }

        public IProject GetOrAddProject(FileInfo path)
        {
            if (_projectCache.TryGetValue(path.FullName, out var cached))
            {
                return cached;
            }

            var project = new MSBuildProject(this, _componentIdentifier, _restorer, _comparer, path, _logger);

            _projectCache.Add(path.FullName, project);

            return project;
        }

        public IEnumerable<IProject> EntryPoints
        {
            get
            {
                if (_entryPointPaths is null)
                {
                    yield break;
                }

                foreach (var path in _entryPointPaths)
                {
                    yield return GetOrAddProject(path);
                }
            }

            set
            {
                _entryPointPaths = value.Select(v => v.FileInfo).ToList();
            }
        }

        public IEnumerable<IProject> Projects
        {
            get
            {
                if (_workspace is null)
                {
                    throw new InvalidOperationException("Context has not been initialized");
                }

                foreach (var project in _workspace.CurrentSolution.Projects)
                {
                    if (project.FilePath is null)
                    {
                        _logger.LogWarning("Found a project with no file path {Project}", project);
                    }
                    else
                    {
                        yield return GetOrAddProject(new FileInfo(project.FilePath));
                    }
                }
            }
        }

        private Dictionary<string, string> CreateProperties()
        {
            var properties = new Dictionary<string, string>();

            if (_vsPath is not null)
            {
                properties.Add("VSINSTALLDIR", _vsPath);
                properties.Add("MSBuildExtensionsPath32", Path.Combine(_vsPath, "MSBuild"));
            }

            return properties;
        }

        public async ValueTask<Workspace> InitializeWorkspace(CancellationToken token)
            => await GetMsBuildWorkspaceAsync(token).ConfigureAwait(false);

        private async ValueTask<MSBuildWorkspace> GetMsBuildWorkspaceAsync(CancellationToken token)
        {
            if (_workspace is null)
            {
                var properties = CreateProperties();
                var workspace = MSBuildWorkspace.Create(properties);

                workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

                if (InputIsSolution)
                {
                    await workspace.OpenSolutionAsync(InputPath, cancellationToken: token).ConfigureAwait(false);
                }
                else
                {
                    await workspace.OpenProjectAsync(InputPath, cancellationToken: token).ConfigureAwait(false);
                }

                _workspace = workspace;
            }

            return _workspace;
        }

        private void UnloadWorkspace()
        {
            ProjectCollection.UnloadAllProjects();
            _projectCache.Clear();
            _workspace?.CloseSolution();
            _workspace?.Dispose();
            _workspace = null;
        }

        public async ValueTask ReloadWorkspaceAsync(CancellationToken token)
        {
            UnloadWorkspace();

            await InitializeWorkspace(token).ConfigureAwait(false);
        }

        private void Workspace_WorkspaceFailed(object? sender, WorkspaceDiagnosticEventArgs e)
        {
            var diagnostic = e.Diagnostic!;

            _logger.LogDebug("[{Level}] Problem loading file in MSBuild workspace {Message}", diagnostic.Kind, diagnostic.Message);
        }

        public IDictionary<string, string> GlobalProperties { get; }

        public IProject? CurrentProject
        {
            get
            {
                if (_projectPath is null)
                {
                    return null;
                }

                return GetOrAddProject(_projectPath);
            }
        }

        public void SetCurrentProject(IProject? project)
        {
            _projectPath = project?.FileInfo;
        }

        public bool UpdateSolution(Solution updatedSolution)
        {
            if (_workspace is null)
            {
                _logger.LogWarning("Cannot update solution if no workspace is loaded.");
                return false;
            }

            if (_workspace.TryApplyChanges(updatedSolution))
            {
                _logger.LogDebug("Source successfully updated");
                return true;
            }
            else
            {
                _logger.LogDebug("Failed to apply changes to source");
                return false;
            }
        }
    }
}

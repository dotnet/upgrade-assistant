// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly string _path;
        private readonly ILogger<MSBuildWorkspaceUpgradeContext> _logger;
        private readonly string? _vsPath;
        private readonly Dictionary<string, IProject> _projectCache;

        private string? _entryPointPath;
        private string? _projectPath;

        private MSBuildWorkspace? _workspace;

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

        public ITargetFrameworkMonikerFactory TfmFactory { get; }

        public bool IsComplete { get; set; }

        public MSBuildWorkspaceUpgradeContext(
            UpgradeOptions options,
            ITargetFrameworkMonikerFactory tfmFactory,
            IVisualStudioFinder vsFinder,
            ILogger<MSBuildWorkspaceUpgradeContext> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _projectCache = new Dictionary<string, IProject>(StringComparer.OrdinalIgnoreCase);
            _path = options.ProjectPath;
            TfmFactory = tfmFactory ?? throw new ArgumentNullException(nameof(tfmFactory));
            _logger = logger;

            var vsPath = vsFinder.GetLatestVisualStudioPath();

            if (vsPath is null)
            {
                throw new UpgradeException("Could not find a Visual Studio install to use for upgrade.");
            }

            _vsPath = vsPath;

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

        public IProject GetOrAddProject(string path)
        {
            if (_projectCache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var project = new MSBuildProject(this, path, _logger);

            _projectCache.Add(path, project);

            return project;
        }

        public IProject? EntryPoint
        {
            get
            {
                if (_entryPointPath is null)
                {
                    return null;
                }

                return GetOrAddProject(_entryPointPath);
            }
        }

        public void SetEntryPoint(IProject? entryPoint) => _entryPointPath = entryPoint?.FilePath;

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
                        yield return GetOrAddProject(project.FilePath);
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

                if (_path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                {
                    await workspace.OpenSolutionAsync(_path, cancellationToken: token).ConfigureAwait(false);
                }
                else
                {
                    var project = await workspace.OpenProjectAsync(_path, cancellationToken: token).ConfigureAwait(false);

                    _projectPath = project.FilePath;
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

            if (string.IsNullOrWhiteSpace(_projectPath))
            {
                return;
            }

            foreach (var project in Projects)
            {
                if (string.Equals(project.FilePath, _projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    SetCurrentProject(project);
                    return;
                }
            }
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

        public void SetCurrentProject(IProject? project) => _projectPath = project?.FilePath;

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

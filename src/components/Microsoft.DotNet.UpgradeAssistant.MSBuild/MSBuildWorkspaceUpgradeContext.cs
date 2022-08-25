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
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal sealed class MSBuildWorkspaceUpgradeContext : IUpgradeContext, IDisposable
    {
        private readonly ILogger<MSBuildWorkspaceUpgradeContext> _logger;
        private readonly Dictionary<string, IProject> _projectCache;
        private readonly IOptions<WorkspaceOptions> _options;
        private readonly Func<MSBuildWorkspaceUpgradeContext, FileInfo, MSBuildProject> _projectFactory;
        private List<FileInfo>? _entryPointPaths;
        private FileInfo? _projectPath;

        private MSBuildWorkspace? _workspace;

        public string InputPath => _options.Value.InputPath;

        public ISolutionInfo SolutionInfo { get; }

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

        public string? SolutionId => SolutionInfo.SolutionId;

        public bool IsComplete { get; set; }

        public UpgradeStep? CurrentStep { get; set; }

        public MSBuildWorkspaceUpgradeContext(
            IOptions<WorkspaceOptions> options,
            Factories factories,
            Func<MSBuildWorkspaceUpgradeContext, FileInfo, MSBuildProject> projectFactory,
            ILogger<MSBuildWorkspaceUpgradeContext> logger)
        {
            _projectFactory = projectFactory ?? throw new ArgumentNullException(nameof(projectFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _projectCache = new Dictionary<string, IProject>(StringComparer.OrdinalIgnoreCase);

            Properties = new UpgradeContextProperties();
            SolutionInfo = factories.CreateSolutionInfo(InputPath);
            GlobalProperties = CreateProperties(options.Value);
            ProjectCollection = new ProjectCollection(globalProperties: GlobalProperties);
        }

        public ProjectCollection ProjectCollection { get; }

        public void Dispose()
        {
            _workspace?.Dispose();
            _workspace = null;
            ProjectCollection.Dispose();
        }

        public IProject GetProject(string path)
        {
            // Normalize to ensure we're always getting the same path
            var normalizedPath = Path.GetFullPath(path);

            if (_projectCache.TryGetValue(normalizedPath, out var cached))
            {
                return cached;
            }

            var project = _projectFactory(this, new FileInfo(normalizedPath));

            _projectCache.Add(normalizedPath, project);

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
                    yield return GetProject(path.FullName);
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
                        yield return GetProject(project.FilePath);
                    }
                }
            }
        }

        private static Dictionary<string, string> CreateProperties(WorkspaceOptions options)
        {
            var properties = new Dictionary<string, string>();

            if (options.VisualStudioPath is string vsPath)
            {
                properties.Add("VSINSTALLDIR", vsPath);
                properties.Add("MSBuildExtensionsPath32", Path.Combine(vsPath, "MSBuild"));
                properties.Add("MSBuildExtensionsPath", Path.Combine(vsPath, "MSBuild"));
            }

            if (options.VisualStudioVersion is int version)
            {
                properties.Add("VisualStudioVersion", $"{version}.0");
            }

            return properties;
        }

        public async ValueTask<Workspace> InitializeWorkspace(CancellationToken token)
            => await GetMsBuildWorkspaceAsync(token).ConfigureAwait(false);

        private async ValueTask<MSBuildWorkspace> GetMsBuildWorkspaceAsync(CancellationToken token)
        {
            if (_workspace is null)
            {
                var workspace = MSBuildWorkspace.Create(GlobalProperties);

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

                return GetProject(_projectPath.FullName);
            }
        }

        public IUpgradeContextProperties Properties { get; }

        public ICollector<OutputResultDefinition> Results { get; } = new Collector<OutputResultDefinition>();

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.MSBuild
{
    internal sealed class MSBuildWorkspaceMigrationContext : IMigrationContext, IDisposable
    {
        private readonly string _path;
        private readonly ILogger<MSBuildWorkspaceMigrationContext> _logger;
        private readonly string? _vsPath;
        private readonly Dictionary<string, MSBuildProject> _projectCache;

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

        public MSBuildWorkspaceMigrationContext(
            MigrateOptions options,
            IVisualStudioFinder vsFinder,
            ILogger<MSBuildWorkspaceMigrationContext> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _projectCache = new Dictionary<string, MSBuildProject>(StringComparer.OrdinalIgnoreCase);
            _path = options.ProjectPath;
            _logger = logger;

            var vsPath = vsFinder.GetLatestVisualStudioPath();

            if (vsPath is null)
            {
                throw new MigrationException("Could not find a Visual Studio install to use for upgrade.");
            }

            _vsPath = vsPath;

            ProjectCollection = new ProjectCollection(globalProperties: CreateProperties());
        }

        public ProjectCollection ProjectCollection { get; }

        public void Dispose()
        {
            _workspace?.Dispose();
            _workspace = null;
            ProjectCollection.Dispose();
        }

        public MSBuildProject GetOrAddProject(string path)
        {
            if (_projectCache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var created = new MSBuildProject(this, path, _logger);

            _projectCache.Add(path, created);

            return created;
        }

        public IProject? EntryPoint { get; set; }

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

        public void UnloadWorkspace()
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
                    Project = project;
                    return;
                }
            }
        }

        private void Workspace_WorkspaceFailed(object? sender, WorkspaceDiagnosticEventArgs e)
        {
            var diagnostic = e.Diagnostic!;

            _logger.LogDebug("[{Level}] Problem loading file in MSBuild workspace {Message}", diagnostic.Kind, diagnostic.Message);
        }

        public async IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties([EnumeratorCancellation] CancellationToken token)
        {
            var ws = await GetMsBuildWorkspaceAsync(token).ConfigureAwait(false);

            foreach (var property in ws.Properties)
            {
                yield return (property.Key, property.Value);
            }
        }

        public IProject? Project
        {
            get
            {
                if (_projectPath is null)
                {
                    return null;
                }

                return GetOrAddProject(_projectPath);
            }

            set
            {
                _projectPath = value?.FilePath;
            }
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

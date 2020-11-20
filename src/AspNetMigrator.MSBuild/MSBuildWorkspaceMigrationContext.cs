using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.MSBuild
{
    public sealed class MSBuildWorkspaceMigrationContext : IMigrationContext, IDisposable
    {
        private readonly IVisualStudioFinder _vsFinder;
        private readonly string _path;
        private readonly ILogger<MSBuildWorkspaceMigrationContext> _logger;

        private ProjectId? _projectId;
        private MSBuildWorkspace? _workspace;

        public MSBuildWorkspaceMigrationContext(
            MigrateOptions options,
            IVisualStudioFinder vsFinder,
            ILogger<MSBuildWorkspaceMigrationContext> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _vsFinder = vsFinder;
            _path = options.ProjectPath;
            _logger = logger;
        }

        public void Dispose()
        {
            _workspace?.Dispose();
            _workspace = null;
        }

        public async ValueTask<ProjectId?> GetProjectIdAsync(CancellationToken token)
        {
            // Ensure workspace is available
            await GetWorkspaceAsync(token).ConfigureAwait(false);

            return _projectId;
        }

        public ValueTask SetProjectAsync(ProjectId projectId, CancellationToken token)
        {
            _projectId = projectId;

            return ValueTask.CompletedTask;
        }

        private Dictionary<string, string> CreateProperties()
        {
            var properties = new Dictionary<string, string>();
            var vs = _vsFinder.GetLatestVisualStudioPath();

            if (vs is not null)
            {
                properties.Add("VSINSTALLDIR", vs);
                properties.Add("MSBuildExtensionsPath32", Path.Combine(vs, "MSBuild"));
            }

            return properties;
        }

        public async ValueTask<Workspace> GetWorkspaceAsync(CancellationToken token)
            => await GetMsBuildWorkspaceAsync(token).ConfigureAwait(false);

        public async ValueTask<MSBuildWorkspace> GetMsBuildWorkspaceAsync(CancellationToken token)
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

                    _projectId = project.Id;
                }

                _workspace = workspace;
            }

            return _workspace;
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
    }
}

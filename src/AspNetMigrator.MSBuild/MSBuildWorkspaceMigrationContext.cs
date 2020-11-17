using System;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace AspNetMigrator.MSBuild
{
    public sealed class MSBuildWorkspaceMigrationContext : IMigrationContext, IDisposable
    {
        private readonly string _path;

        private Project _project;
        private Workspace _workspace;

        public MSBuildWorkspaceMigrationContext(string path)
        {
            _path = path;
        }

        public Migrator Migrator { get; init; }

        public void Dispose()
        {
            _workspace?.Dispose();
            _workspace = null;
        }

        public async ValueTask<ProjectId> GetProjectIdAsync(CancellationToken token)
        {
            // Ensure workspace is available
            await GetWorkspaceAsync(token).ConfigureAwait(false);

            return _project.Id;
        }

        public async ValueTask<Workspace> GetWorkspaceAsync(CancellationToken token)
        {
            if (_workspace is null)
            {
                var workspace = MSBuildWorkspace.Create();
                _project = await workspace.OpenProjectAsync(_path, cancellationToken: token).ConfigureAwait(false);
                _workspace = workspace;
            }

            return _workspace;
        }
    }
}

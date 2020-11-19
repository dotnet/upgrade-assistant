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

        private ProjectId? _projectId;
        private Workspace? _workspace;

        public MSBuildWorkspaceMigrationContext(string path)
        {
            _path = path;
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

        public async ValueTask<Workspace> GetWorkspaceAsync(CancellationToken token)
        {
            if (_workspace is null)
            {
                var workspace = MSBuildWorkspace.Create();

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
    }
}

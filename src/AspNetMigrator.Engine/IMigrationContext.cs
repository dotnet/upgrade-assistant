using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator.Engine
{
    public interface IMigrationContext
    {
        ValueTask<ProjectId?> GetProjectIdAsync(CancellationToken token);

        ValueTask SetProjectAsync(ProjectId projectId, CancellationToken token);

        ValueTask<Workspace> GetWorkspaceAsync(CancellationToken token);

        async ValueTask<Project?> GetProjectAsync(CancellationToken token)
        {
            var ws = await GetWorkspaceAsync(token).ConfigureAwait(false);
            var projectId = await GetProjectIdAsync(token).ConfigureAwait(false);

            return ws.CurrentSolution.GetProject(projectId);
        }
    }
}

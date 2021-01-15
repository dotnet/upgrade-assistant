using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator
{
    public interface IMigrationContext : IDisposable
    {
        ValueTask<ProjectId?> GetProjectIdAsync(CancellationToken token);

        ValueTask SetProjectAsync(ProjectId? projectId, CancellationToken token);

        IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties(CancellationToken token);

        ValueTask<Workspace> GetWorkspaceAsync(CancellationToken token);

        public void UnloadWorkspace();

        async ValueTask<Project?> GetProjectAsync(CancellationToken token)
        {
            var ws = await GetWorkspaceAsync(token).ConfigureAwait(false);
            var projectId = await GetProjectIdAsync(token).ConfigureAwait(false);

            return ws.CurrentSolution.GetProject(projectId);
        }

        async ValueTask<string?> GetProjectPathAsync(CancellationToken token)
        {
            var project = await GetProjectAsync(token).ConfigureAwait(false);

            return project?.FilePath;
        }

        async ValueTask<ProjectRootElement> GetProjectRootElementAsync(CancellationToken token)
        {
            var projectRoot = ProjectRootElement.Open(await GetProjectPathAsync(token).ConfigureAwait(false));
            projectRoot.Reload(false); // Reload to make sure we're not seeing an old cached version of the project

            return projectRoot;
        }

        async ValueTask ReloadWorkspaceAsync(CancellationToken token)
        {
            var projectPath = await GetProjectPathAsync(token).ConfigureAwait(false);
            UnloadWorkspace();
            var ws = await GetWorkspaceAsync(token).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                await SetProjectAsync(ws.CurrentSolution.Projects.First(p => projectPath.Equals(p.FilePath, StringComparison.OrdinalIgnoreCase)).Id, token).ConfigureAwait(false);
            }
        }
    }
}

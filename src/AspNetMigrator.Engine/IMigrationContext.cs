using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator.Engine
{
    public interface IMigrationContext : IDisposable
    {
        ValueTask<ProjectId?> GetProjectIdAsync(CancellationToken token);

        ValueTask SetProjectAsync(ProjectId projectId, CancellationToken token);

        IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties(CancellationToken token);

        ValueTask<Workspace> GetWorkspaceAsync(CancellationToken token);

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
    }
}

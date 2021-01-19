using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator
{
    public interface IMigrationContext : IDisposable
    {
        ValueTask<IProject?> GetProjectAsync(CancellationToken token);

        IAsyncEnumerable<IProject> GetProjects(CancellationToken token);

        bool UpdateSolution(Solution updatedSolution);

        void SetProject(IProject? project);

        IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties(CancellationToken token);

        async ValueTask<string?> GetProjectPathAsync(CancellationToken token)
        {
            var project = await GetProjectAsync(token).ConfigureAwait(false);

            if (project is null)
            {
                return null;
            }

            return project.GetRoslynProject()?.FilePath;
        }

        async ValueTask<ProjectRootElement> GetProjectRootElementAsync(CancellationToken token)
        {
            var projectRoot = ProjectRootElement.Open(await GetProjectPathAsync(token).ConfigureAwait(false));
            projectRoot.Reload(false); // Reload to make sure we're not seeing an old cached version of the project

            return projectRoot;
        }

        ValueTask ReloadWorkspaceAsync(CancellationToken token);
    }
}

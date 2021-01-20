using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        ValueTask ReloadWorkspaceAsync(CancellationToken token);
    }
}

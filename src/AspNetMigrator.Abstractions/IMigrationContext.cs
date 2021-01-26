using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator
{
    public interface IMigrationContext : IDisposable
    {
        IProject? Project { get; set; }

        IEnumerable<IProject> Projects { get; }

        bool UpdateSolution(Solution updatedSolution);

        IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties(CancellationToken token);

        public ValueTask ReloadWorkspaceAsync(CancellationToken token);
    }
}

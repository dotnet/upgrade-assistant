using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator
{
    public interface IMigrationContext : IDisposable
    {
        IProject? EntryPoint { get; set; }

        TargetFrameworkMoniker? EntryPointTargetTFM { get; }

        IProject? Project { get; set; }

        TargetFrameworkMoniker? TargetTFM { get; }

        IEnumerable<IProject> Projects { get; }

        bool UpdateSolution(Solution updatedSolution);

        IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties(CancellationToken token);

        ValueTask ReloadWorkspaceAsync(CancellationToken token);
    }
}

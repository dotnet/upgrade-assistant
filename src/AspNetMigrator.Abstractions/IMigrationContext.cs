using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator
{
    public interface IMigrationContext : IDisposable
    {
        IProject? EntryPoint { get; }

        TargetFrameworkMoniker? EntryPointTargetTFM { get; }

        IProject? Project { get; }

        IEnumerable<IProject> Projects { get; }

        TargetFrameworkMoniker? TargetTFM { get; }

        bool UpdateSolution(Solution updatedSolution);

        IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties(CancellationToken token);

        ValueTask ReloadWorkspaceAsync(CancellationToken token);

        ValueTask SetEntryPointAsync(IProject? entryPoint, CancellationToken token);

        ValueTask SetProjectAsync(IProject? entryPoint, CancellationToken token);
    }
}

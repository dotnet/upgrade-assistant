using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.UpgradeAssistant
{
    public interface IMigrationContext : IDisposable
    {
        UpgradeProjectInfo? EntryPoint { get; }

        void SetEntryPoint(IProject? entryPoint);

        UpgradeProjectInfo? CurrentProject { get; }

        void SetCurrentProject(IProject? project);

        IEnumerable<IProject> Projects { get; }

        bool UpdateSolution(Solution updatedSolution);

        IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties(CancellationToken token);

        ValueTask ReloadWorkspaceAsync(CancellationToken token);
    }
}

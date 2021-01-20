using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator.TestHelpers
{
    public sealed class NullMigrationContext : IMigrationContext
    {
        public IEnumerable<IProject> Projects => throw new NotImplementedException();

        public void Dispose() { }

        public IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties(CancellationToken token) => AsyncEnumerable.Empty<(string, string)>();

        public ValueTask ReloadWorkspaceAsync(CancellationToken token) => throw new NotImplementedException();

        public bool UpdateSolution(Solution updatedSolution) => throw new NotImplementedException();

        public IProject? Project { get; set; }
    }
}

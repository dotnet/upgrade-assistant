using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator.TestHelpers
{
    public sealed class NullMigrationContext : IMigrationContext
    {
        public ICollection<string> CompletedProjects { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IAsyncEnumerable<IProject> GetProjects(CancellationToken token) => throw new NotImplementedException();

        public void Dispose() { }

        public ValueTask<IProject?> GetProjectAsync(CancellationToken token) => throw new NotImplementedException();

        public IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties(CancellationToken token) => AsyncEnumerable.Empty<(string, string)>();

        public ValueTask ReloadWorkspaceAsync(CancellationToken token) => throw new NotImplementedException();

        public void SetProject(IProject? project) => throw new NotImplementedException();

        public bool UpdateSolution(Solution updatedSolution) => throw new NotImplementedException();
    }
}

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

        public void Dispose() { }

        public ValueTask<ProjectId?> GetProjectIdAsync(CancellationToken token) => ValueTask.FromResult<ProjectId?>(null);

        public ValueTask<Workspace> GetWorkspaceAsync(CancellationToken token) => ValueTask.FromResult<Workspace>(null!);

        public IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties(CancellationToken token) => AsyncEnumerable.Empty<(string, string)>();

        public ValueTask SetProjectAsync(ProjectId? projectId, CancellationToken token) => ValueTask.CompletedTask;

        public void UnloadWorkspace() { }
    }
}

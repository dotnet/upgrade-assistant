using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AspNetMigrator.Engine
{
    public interface IMigrationContext
    {
        ValueTask<ProjectId> GetProjectIdAsync(CancellationToken token);

        ValueTask<Workspace> GetWorkspaceAsync(CancellationToken token);
    }
}

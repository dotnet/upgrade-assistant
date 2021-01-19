using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.ConfigUpdater
{
    public interface IConfigUpdater
    {
        string Title { get; }

        string Description { get; }

        BuildBreakRisk Risk { get; }

        Task<bool> ApplyAsync(IMigrationContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token);

        Task<bool> IsApplicableAsync(IMigrationContext context, ImmutableArray<ConfigFile> configFiles, CancellationToken token);
    }
}

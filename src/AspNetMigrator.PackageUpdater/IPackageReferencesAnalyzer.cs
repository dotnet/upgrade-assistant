using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.PackageUpdater
{
    public interface IPackageReferencesAnalyzer
    {
        string Name { get; }

        Task<PackageAnalysisState> AnalyzeAsync(IMigrationContext context, IEnumerable<NuGetReference> references, PackageAnalysisState? state, CancellationToken token);
    }
}

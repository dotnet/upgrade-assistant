using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.PackageUpdater
{
    public class PackageAnalysisState
    {
        public IMigrationContext Context { get; }

        public string? LockFilePath { get; set; }

        public string? PackageCachePath { get; set; }

        public IList<NuGetReference> PackagesToAdd { get; }

        public IList<NuGetReference> PackagesToRemove { get; }

        public bool Failed { get; set; }

        public bool PossibleBreakingChangeRecommended { get; set; }

        public bool ChangesRecommended => PackagesToAdd.Any() || PackagesToRemove.Any();

        public PackageAnalysisState(IMigrationContext context)
        {
            PackagesToRemove = new List<NuGetReference>();
            PackagesToAdd = new List<NuGetReference>();
            Failed = false;
            PossibleBreakingChangeRecommended = false;
            Context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        public async Task<bool> EnsurePackagesRestoredAsync(IPackageRestorer packageRestorer, CancellationToken token)
        {
            if (packageRestorer is null)
            {
                throw new System.ArgumentNullException(nameof(packageRestorer));
            }

            if (LockFilePath is null || PackageCachePath is null || Failed)
            {
                var restoreOutput = await packageRestorer.RestorePackagesAsync(Context, token).ConfigureAwait(false);
                if (restoreOutput.LockFilePath is null)
                {
                    Failed = true;
                    return false;
                }
                else
                {
                    (LockFilePath, PackageCachePath) = (restoreOutput.LockFilePath, restoreOutput.PackageCachePath);
                    Failed = false;
                }
            }

            return true;
        }
    }
}

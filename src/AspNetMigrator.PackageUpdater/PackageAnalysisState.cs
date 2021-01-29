﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.PackageUpdater
{
    public class PackageAnalysisState
    {
        public string LockFilePath { get; private set; } = default!;

        public string PackageCachePath { get; private set; } = default!;

        public IList<NuGetReference> PackagesToAdd { get; }

        public IList<NuGetReference> PackagesToRemove { get; }

        public bool Failed { get; set; }

        public bool PossibleBreakingChangeRecommended { get; set; }

        public bool ChangesRecommended => PackagesToAdd.Any() || PackagesToRemove.Any();

        private PackageAnalysisState()
        {
            PackagesToRemove = new List<NuGetReference>();
            PackagesToAdd = new List<NuGetReference>();
            Failed = false;
            PossibleBreakingChangeRecommended = false;
        }

        /// <summary>
        /// Creates a new analysis state object for a given migration context. This involves restoring packages for the context's current project.
        /// </summary>
        /// <param name="context">The migration context to create an analysis state for.</param>
        /// <param name="packageRestorer">The package restorer to use to restore packages for the context's project and generate a lock file.</param>
        /// <returns>A new PackageAnalysisState instance for the specified context.</returns>
        public static async Task<PackageAnalysisState> CreateAsync(IMigrationContext context, IPackageRestorer packageRestorer, CancellationToken token)
        {
            if (context is null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            if (packageRestorer is null)
            {
                throw new System.ArgumentNullException(nameof(packageRestorer));
            }

            var ret = new PackageAnalysisState();
            await ret.PopulatePackageRestoreState(context, packageRestorer, token).ConfigureAwait(false);
            return ret;
        }

        private async Task<bool> PopulatePackageRestoreState(IMigrationContext context, IPackageRestorer packageRestorer, CancellationToken token)
        {
            if (LockFilePath is null || PackageCachePath is null || Failed)
            {
                var restoreOutput = await packageRestorer.RestorePackagesAsync(context, token).ConfigureAwait(false);
                if (restoreOutput.LockFilePath is null || restoreOutput.PackageCachePath is null)
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UpgradeAssistant;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;

namespace AspNetMigrator.PackageUpdater.Analyzers
{
    public class TransitiveReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private readonly ILogger<TransitiveReferenceAnalyzer> _logger;

        public string Name => "Transitive reference analyzer";

        public TransitiveReferenceAnalyzer(ILogger<TransitiveReferenceAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<PackageAnalysisState> AnalyzeAsync(IEnumerable<NuGetReference> references, PackageAnalysisState state, CancellationToken token)
        {
            if (references is null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var tfm = NuGetFramework.Parse(state.CurrentTFM.Name);

            _logger.LogDebug("Searching lock file for dependencies for {TFM}", tfm.DotNetFrameworkName);

            var lockFileTarget = LockFileUtilities.GetLockFile(state.LockFilePath, NuGet.Common.NullLogger.Instance)
                .Targets
                .First(t => t.TargetFramework.DotNetFrameworkName.Equals(tfm.DotNetFrameworkName, StringComparison.Ordinal));

            // If the package is referenced transitively, mark for removal
            foreach (var packageReference in references.Where(r => !state.PackagesToRemove.Contains(r)))
            {
                if (lockFileTarget.Libraries.Any(l => l.Dependencies.Any(d => ReferenceSatisfiesDependency(d, packageReference, true))))
                {
                    _logger.LogInformation("Marking package {PackageName} for removal because it appears to be a transitive dependency", packageReference.Name);
                    state.PackagesToRemove.Add(packageReference);
                    continue;
                }
            }

            return Task.FromResult(state);
        }

        private static bool ReferenceSatisfiesDependency(PackageDependency dependency, NuGetReference packageReference, bool minVersionMatchOnly)
        {
            // If the dependency's name doesn't match the reference's name, return false
            if (!dependency.Id.Equals(packageReference.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var packageVersion = packageReference.GetNuGetVersion();
            if (packageVersion == null)
            {
                throw new InvalidOperationException("Package references from a lock file should always have a specific version");
            }

            // Return false if the reference's version falls outside of the dependency range
            var versionRange = dependency.VersionRange;
            if (versionRange.HasLowerBound && packageVersion < versionRange.MinVersion)
            {
                return false;
            }

            if (versionRange.HasUpperBound && packageVersion > versionRange.MaxVersion)
            {
                return false;
            }

            // In some cases (looking for transitive dependencies), it's interesting to only match packages that are the minimum version
            if (minVersionMatchOnly && versionRange.HasLowerBound && packageVersion != versionRange.MinVersion)
            {
                return false;
            }

            // Otherwise, return true
            return true;
        }
    }
}

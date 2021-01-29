using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;

namespace AspNetMigrator.PackageUpdater.Analyzers
{
    public class TransitiveReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private readonly NuGetFramework _targetFramework;
        private readonly IPackageRestorer _packageRestorer;
        private readonly ILogger<TransitiveReferenceAnalyzer> _logger;

        public string Name => "Transitive reference analyzer";

        public TransitiveReferenceAnalyzer(MigrateOptions options, IPackageRestorer packageRestorer, ILogger<TransitiveReferenceAnalyzer> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _targetFramework = NuGetFramework.Parse(options.TargetFramework);
            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PackageAnalysisState> AnalyzeAsync(IMigrationContext context, IEnumerable<NuGetReference> references, PackageAnalysisState? state, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (references is null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            if (state is null)
            {
                state = new PackageAnalysisState(context);
            }

            if (!await state.EnsurePackagesRestoredAsync(_packageRestorer, token).ConfigureAwait(false))
            {
                _logger.LogCritical("Unable to restore packages for project {ProjectPath}", context.Project?.FilePath);
                return state;
            }

            var lockFileTarget = GetLockFileTarget(state.LockFilePath!);

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

            return state;
        }

        private LockFileTarget GetLockFileTarget(string lockFilePath) =>
            LockFileUtilities.GetLockFile(lockFilePath, NuGet.Common.NullLogger.Instance)
                .Targets.First(t => t.TargetFramework.DotNetFrameworkName.Equals(_targetFramework.DotNetFrameworkName, StringComparison.Ordinal));

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

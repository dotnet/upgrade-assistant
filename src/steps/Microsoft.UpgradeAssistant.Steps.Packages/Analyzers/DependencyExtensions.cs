using System;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;

namespace Microsoft.UpgradeAssistant.Steps.Packages.Analyzers
{
    internal static class DependencyExtensions
    {
        public static bool IsTransitivelyAvailable(this PackageAnalysisState state, NuGetReference nugetReference)
        {
            var tfm = NuGetFramework.Parse(state.CurrentTFM.Name);
            var lockFileTarget = LockFileUtilities.GetLockFile(state.LockFilePath, NuGet.Common.NullLogger.Instance)
                .Targets
                .First(t => t.TargetFramework.DotNetFrameworkName.Equals(tfm.DotNetFrameworkName, StringComparison.Ordinal));

            return lockFileTarget.Libraries.Any(l => l.Dependencies.Any(d => d.ReferenceSatisfiesDependency(nugetReference, true)));
        }

        private static bool ReferenceSatisfiesDependency(this PackageDependency dependency, NuGetReference packageReference, bool minVersionMatchOnly)
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

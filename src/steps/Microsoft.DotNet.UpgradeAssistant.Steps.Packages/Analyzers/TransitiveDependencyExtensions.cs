// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    internal static class TransitiveDependencyExtensions
    {
        public static bool IsTransitivelyAvailable(this PackageAnalysisState state, string packageName)
            => state.ContainsDependency(d => string.Equals(packageName, d.Id, StringComparison.OrdinalIgnoreCase));

        public static bool IsTransitiveDependency(this PackageAnalysisState state, NuGetReference nugetReference)
            => state.ContainsDependency(d => d.ReferenceSatisfiesDependency(nugetReference, true));

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

        private static bool ContainsDependency(this PackageAnalysisState state, Func<PackageDependency, bool> filter)
            => state.GetAllDependencies()
                    .Any(l => l.Dependencies.Any(d => filter(d)));

        private static IEnumerable<LockFileTargetLibrary> GetAllDependencies(this PackageAnalysisState state)
        {
            var tfm = NuGetFramework.Parse(state.CurrentTFM.Name);
            var lockFileTarget = LockFileUtilities.GetLockFile(state.LockFilePath, NuGet.Common.NullLogger.Instance)
                .Targets
                .First(t => t.TargetFramework.DotNetFrameworkName.Equals(tfm.DotNetFrameworkName, StringComparison.Ordinal));

            return lockFileTarget.Libraries;
        }
    }
}

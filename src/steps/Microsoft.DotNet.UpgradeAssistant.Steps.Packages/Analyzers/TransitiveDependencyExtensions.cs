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
        public static bool IsTransitivelyAvailable(this IProject project, string packageName)
            => project.TargetFrameworks.Any(tfm => project.IsTransitivelyAvailable(packageName, tfm));

        public static bool IsTransitivelyAvailable(this IProject project, string packageName, TargetFrameworkMoniker tfm)
            => project.ContainsDependency(tfm, d => string.Equals(packageName, d.Id, StringComparison.OrdinalIgnoreCase));

        public static bool IsTransitiveDependency(this IProject project, NuGetReference nugetReference, TargetFrameworkMoniker tfm)
            => project.ContainsDependency(tfm, d => d.ReferenceSatisfiesDependency(nugetReference, true));

        public static bool IsTransitiveDependency(this IProject project, NuGetReference nugetReference)
            => project.TargetFrameworks.Any(tfm => project.ContainsDependency(tfm, d => d.ReferenceSatisfiesDependency(nugetReference, true)));

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

        private static bool ContainsDependency(this IProject project, TargetFrameworkMoniker tfm, Func<PackageDependency, bool> filter)
            => project.GetAllDependencies(tfm).Any(l => l.Dependencies.Any(d => filter(d)));

        private static IEnumerable<LockFileTargetLibrary> GetAllDependencies(this IProject project, TargetFrameworkMoniker tfm)
        {
            var parsed = NuGetFramework.Parse(tfm.Name);
            var lockFileTarget = LockFileUtilities.GetLockFile(project.LockFilePath, NuGet.Common.NullLogger.Instance)
                .Targets
                .First(t => t.TargetFramework.DotNetFrameworkName.Equals(parsed.DotNetFrameworkName, StringComparison.Ordinal));

            return lockFileTarget.Libraries;
        }
    }
}

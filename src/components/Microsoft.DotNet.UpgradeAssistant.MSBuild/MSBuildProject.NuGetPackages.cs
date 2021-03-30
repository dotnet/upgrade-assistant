// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal partial class MSBuildProject : INuGetReferences
    {
        public INuGetReferences NuGetReferences => this;

        public bool IsTransitivelyAvailable(string packageName)
            => TargetFrameworks.Any(tfm => ContainsDependency(tfm, d => string.Equals(packageName, d.Id, StringComparison.OrdinalIgnoreCase)));

        public bool IsTransitiveDependency(NuGetReference nugetReference)
            => TargetFrameworks.Any(tfm => ContainsDependency(tfm, d => ReferenceSatisfiesDependency(d, nugetReference, true)));

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

        private bool ContainsDependency(TargetFrameworkMoniker tfm, Func<PackageDependency, bool> filter)
            => GetAllDependencies(tfm).Any(l => l.Dependencies.Any(d => filter(d)));

        private IEnumerable<LockFileTargetLibrary> GetAllDependencies(TargetFrameworkMoniker tfm)
        {
            var parsedTfm = NuGetFramework.Parse(tfm.Name);
            var lockFile = LockFileUtilities.GetLockFile(LockFilePath, NuGet.Common.NullLogger.Instance);

            if (lockFile is null)
            {
                return Enumerable.Empty<LockFileTargetLibrary>();
            }

            return lockFile.Targets
                .First(t => t.TargetFramework.DotNetFrameworkName.Equals(parsedTfm.DotNetFrameworkName, StringComparison.Ordinal))
                .Libraries;
        }
    }
}

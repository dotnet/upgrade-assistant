// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal partial class MSBuildProject : INuGetReferences
    {
        public INuGetReferences NuGetReferences => this;

        public NugetPackageFormat PackageReferenceFormat
        {
            get
            {
                if (GetPackagesConfigPath() is not null)
                {
                    return NugetPackageFormat.PackageConfig;
                }
                else if (ProjectRoot.GetAllPackageReferences().ToList() is IEnumerable<Build.Construction.ProjectItemElement> list && list.Any())
                {
                    return NugetPackageFormat.PackageReference;
                }
                else
                {
                    return NugetPackageFormat.None;
                }
            }
        }

        private string? GetPackagesConfigPath() => FindFiles(ProjectItemType.Content, "packages.config").FirstOrDefault();

        public IEnumerable<NuGetReference> PackageReferences
        {
            get
            {
                var packagesConfig = GetPackagesConfigPath();

                if (packagesConfig is null)
                {
                    var packages = ProjectRoot.GetAllPackageReferences();

                    return packages.Select(p => p.AsNuGetReference());
                }
                else
                {
                    return PackageConfig.GetPackages(packagesConfig);
                }
            }
        }

        public IAsyncEnumerable<NuGetReference> GetTransitivePackageReferencesAsync(TargetFrameworkMoniker tfm, CancellationToken token)
            => GetAllDependenciesAsync(tfm, token).Select(l => new NuGetReference(l.Name, l.Version.ToNormalizedString()));

        public ValueTask<bool> IsTransitivelyAvailableAsync(string packageName, CancellationToken token)
            => TargetFrameworks.ToAsyncEnumerable().AnyAwaitAsync(tfm => ContainsDependencyAsync(tfm, d => string.Equals(packageName, d.Id, StringComparison.OrdinalIgnoreCase), token), cancellationToken: token);

        public ValueTask<bool> IsTransitiveDependencyAsync(NuGetReference nugetReference, CancellationToken token)
            => TargetFrameworks.ToAsyncEnumerable().AnyAwaitAsync(tfm => ContainsDependencyAsync(tfm, d => ReferenceSatisfiesDependency(d, nugetReference, true), token), token);

        private static bool ReferenceSatisfiesDependency(PackageDependency dependency, NuGetReference packageReference, bool minVersionMatchOnly)
        {
            // If the dependency's name doesn't match the reference's name, return false
            if (!dependency.Id.Equals(packageReference.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!packageReference.TryGetNuGetVersion(out var packageVersion))
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

        private ValueTask<bool> ContainsDependencyAsync(TargetFrameworkMoniker tfm, Func<PackageDependency, bool> filter, CancellationToken token)
            => GetAllDependenciesAsync(tfm, token).AnyAsync(l => l.Dependencies.Any(d => filter(d)), token);

        private async IAsyncEnumerable<LockFileTargetLibrary> GetAllDependenciesAsync(TargetFrameworkMoniker tfm, [EnumeratorCancellation] CancellationToken token)
        {
            if (!IsRestored)
            {
                throw new InvalidOperationException("Project should have already been restored. Please file an issue at https://github.com/dotnet/upgrade-assistant");
            }

            var parsedTfm = NuGetFramework.Parse(tfm.Name);
            var target = GetLockFileTarget(parsedTfm);

            if (target is null)
            {
                // Break if there are no packages in the project. Otherwise, we end up performing restores too often.
                if (!PackageReferences.Any())
                {
                    yield break;
                }

                await _restorer.RestorePackagesAsync(Context, this, token).ConfigureAwait(false);

                // If the LockFilePath is defined but does not exist, there are no libraries
                if (!File.Exists(LockFilePath))
                {
                    yield break;
                }

                target = GetLockFileTarget(parsedTfm);
            }

            if (target is null)
            {
                throw new InvalidOperationException("Cannot find targets. Please ensure that the project is fully restored.");
            }

            foreach (var library in target.Libraries)
            {
                yield return library;
            }

            LockFileTarget? GetLockFileTarget(NuGetFramework parsedTfm)
            {
                var lockFile = LockFileUtilities.GetLockFile(LockFilePath, NuGet.Common.NullLogger.Instance);

                if (lockFile?.Targets is null)
                {
                    return null;
                }

                return lockFile.Targets
                    .FirstOrDefault(t => t.TargetFramework.DotNetFrameworkName.Equals(parsedTfm.DotNetFrameworkName, StringComparison.Ordinal));
            }
        }

        private bool IsRestored => LockFilePath is not null;

        private string? LockFilePath
        {
            get
            {
                var lockFilePath = Path.Combine(GetPropertyValue("MSBuildProjectExtensionsPath"), "project.assets.json");

                if (string.IsNullOrEmpty(lockFilePath))
                {
                    return null;
                }

                if (!Path.IsPathFullyQualified(lockFilePath))
                {
                    lockFilePath = Path.Combine(FileInfo.DirectoryName ?? string.Empty, lockFilePath);
                }

                return lockFilePath;
            }
        }
    }
}

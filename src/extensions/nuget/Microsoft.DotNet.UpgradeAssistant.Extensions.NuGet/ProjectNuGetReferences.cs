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
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class ProjectNuGetReferences : INuGetReferences
    {
        private readonly IUpgradeContext _context;
        private readonly IProject _project;
        private readonly IPackageRestorer _restorer;
        private readonly ILogger<ProjectNuGetReferences> _logger;

        public ProjectNuGetReferences(
            IUpgradeContext context,
            IProject project,
            IPackageRestorer restorer,
            ILogger<ProjectNuGetReferences> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _restorer = restorer ?? throw new ArgumentNullException(nameof(restorer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public NugetPackageFormat PackageReferenceFormat
        {
            get
            {
                if (GetPackagesConfigPath() is not null)
                {
                    return NugetPackageFormat.PackageConfig;
                }
                else if (_project.PackageReferences.Any())
                {
                    return NugetPackageFormat.PackageReference;
                }
                else
                {
                    return NugetPackageFormat.None;
                }
            }
        }

        private string? GetPackagesConfigPath() => _project.FindFiles("packages.config").FirstOrDefault();

        public IEnumerable<NuGetReference> PackageReferences
        {
            get
            {
                var packagesConfig = GetPackagesConfigPath();

                if (packagesConfig is null)
                {
                    return _project.PackageReferences;
                }
                else
                {
                    return PackageConfig.GetPackages(packagesConfig);
                }
            }
        }

        public IAsyncEnumerable<NuGetReference> GetTransitivePackageReferencesAsync(TargetFrameworkMoniker tfm, CancellationToken token)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            return PackageReferenceFormat switch
            {
                NugetPackageFormat.PackageConfig => PackageReferences.ToAsyncEnumerable(),
                NugetPackageFormat.PackageReference => GetAllPackageReferenceDependenciesAsync(tfm, token).Select(l => new NuGetReference(l.Name, l.Version.ToNormalizedString())),
                _ => AsyncEnumerable.Empty<NuGetReference>()
            };
        }

        public async ValueTask<bool> IsTransitivelyAvailableAsync(string packageName, CancellationToken token)
            => PackageReferences.Any(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase))
            || (PackageReferenceFormat == NugetPackageFormat.PackageReference && await _project.TargetFrameworks.ToAsyncEnumerable().AnyAwaitAsync(tfm => ContainsPackageDependencyAsync(tfm, d => string.Equals(packageName, d.Id, StringComparison.OrdinalIgnoreCase), token), cancellationToken: token).ConfigureAwait(false));

        public async ValueTask<bool> IsTransitiveDependencyAsync(NuGetReference nugetReference, CancellationToken token)
            => PackageReferenceFormat == NugetPackageFormat.PackageReference && await _project.TargetFrameworks.ToAsyncEnumerable().AnyAwaitAsync(tfm => ContainsPackageDependencyAsync(tfm, d => ReferenceSatisfiesDependency(d, nugetReference, true), token), token).ConfigureAwait(false);

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

        private ValueTask<bool> ContainsPackageDependencyAsync(TargetFrameworkMoniker tfm, Func<PackageDependency, bool> filter, CancellationToken token)
            => GetAllPackageReferenceDependenciesAsync(tfm, token).AnyAsync(l => l.Dependencies.Any(d => filter(d)), token);

        private async IAsyncEnumerable<LockFileTargetLibrary> GetAllPackageReferenceDependenciesAsync(TargetFrameworkMoniker tfm, [EnumeratorCancellation] CancellationToken token)
        {
            if (!IsRestored)
            {
                throw new InvalidOperationException("Project should have already been restored. Please file an issue at https://github.com/dotnet/upgrade-assistant");
            }

            if (PackageReferenceFormat != NugetPackageFormat.PackageReference)
            {
                throw new InvalidOperationException("PackageReference restore for transitive dependencies should only happen for PackageReference package reference format");
            }

            var parsedTfm = NuGetFramework.Parse(tfm.Name);
            var target = GetLockFileTarget(parsedTfm);

            if (target is null)
            {
                // Break if there are no packages in the project. Otherwise, we end up performing restores too often.
                if (!PackageReferences.Any())
                {
                    _logger.LogDebug("Skipping restore as no package references exist in project file {Path}", _project.FileInfo.FullName);
                    yield break;
                }

                _logger.LogDebug("Attempting a restore to retrieve missing lock file data {Path}", _project.FileInfo.FullName);

                await _restorer.RestorePackagesAsync(_context, _project, token).ConfigureAwait(false);

                // If the LockFilePath is defined but does not exist, there are no libraries
                if (!File.Exists(LockFilePath))
                {
                    yield break;
                }

                target = GetLockFileTarget(parsedTfm);
            }

            if (target is null)
            {
                _logger.LogError("NuGet target in project.assets.json is still unavailable after restore. Please verify that the project has been restored.");
                throw new UpgradeException("Restore has not restored the expected TFMs. Please review any warnings from dotnet restore.");
            }

            foreach (var library in target.Libraries)
            {
                yield return library;
            }

            LockFileTarget? GetLockFileTarget(NuGetFramework parsedTfm)
            {
                var lockFile = LockFileUtilities.GetLockFile(LockFilePath, global::NuGet.Common.NullLogger.Instance);

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
                var lockFilePath = Path.Combine(_project.GetFile().GetPropertyValue("MSBuildProjectExtensionsPath"), "project.assets.json");

                if (string.IsNullOrEmpty(lockFilePath))
                {
                    return null;
                }

                if (!Path.IsPathFullyQualified(lockFilePath))
                {
                    lockFilePath = Path.Combine(_project.FileInfo.DirectoryName ?? string.Empty, lockFilePath);
                }

                return lockFilePath;
            }
        }
    }
}

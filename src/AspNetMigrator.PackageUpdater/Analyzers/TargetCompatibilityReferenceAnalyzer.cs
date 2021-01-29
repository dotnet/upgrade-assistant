using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;

namespace AspNetMigrator.PackageUpdater.Analyzers
{
    public class TargetCompatibilityReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private readonly IPackageRestorer _packageRestorer;
        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<TargetCompatibilityReferenceAnalyzer> _logger;
        private readonly NuGetFramework _targetFramework;

        public string Name => "Target compatibility reference analyzer";

        public TargetCompatibilityReferenceAnalyzer(MigrateOptions options, IPackageRestorer packageRestorer, IPackageLoader packageLoader, ILogger<TargetCompatibilityReferenceAnalyzer> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _targetFramework = NuGetFramework.Parse(options.TargetFramework);
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

            foreach (var packageReference in references.Where(r => !state.PackagesToRemove.Contains(r)))
            {
                // If the package doesn't target the right framework but a newer version does, mark it for removal and the newer version for addition
                if (await DoesPackageSupportTargetFrameworkAsync(packageReference, state.PackageCachePath!, token).ConfigureAwait(false))
                {
                    _logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", packageReference, _targetFramework);
                    continue;
                }
                else
                {
                    // If the package won't work on the target Framework, check newer versions of the package
                    var updatedReference = await GetUpdatedPackageVersionAsync(packageReference, state.PackageCachePath!, token).ConfigureAwait(false);
                    if (updatedReference == null)
                    {
                        _logger.LogWarning("No version of {PackageName} found that supports {TargetFramework}; leaving unchanged", packageReference.Name, _targetFramework);
                    }
                    else
                    {
                        _logger.LogInformation("Marking package {NuGetPackage} for removal because it doesn't support the target framework but a newer version ({Version}) does", packageReference, updatedReference.Version);
                        var newMajorVersion = updatedReference.GetNuGetVersion()?.Major;
                        var oldMajorVersion = packageReference.GetNuGetVersion()?.Major;

                        if (newMajorVersion != oldMajorVersion)
                        {
                            _logger.LogWarning("Package {NuGetPackage} has been upgraded across major versions ({OldVersion} -> {NewVersion}) which may introduce breaking changes", packageReference.Name, oldMajorVersion, newMajorVersion);
                            state.PossibleBreakingChangeRecommended = true;
                        }

                        state.PackagesToRemove.Add(packageReference);
                        state.PackagesToAdd.Add(updatedReference);
                    }
                }
            }

            return state;
        }

        private async Task<NuGetReference?> GetUpdatedPackageVersionAsync(NuGetReference packageReference, string packageCachePath, CancellationToken token)
        {
            var latestMinorVersions = await _packageLoader.GetNewerVersionsAsync(packageReference.Name, new NuGetVersion(packageReference.GetNuGetVersion()), true, token).ConfigureAwait(false);
            NuGetReference? updatedReference = null;
            foreach (var newerPackage in latestMinorVersions.Select(v => new NuGetReference(packageReference.Name, v.ToString())))
            {
                if (await DoesPackageSupportTargetFrameworkAsync(newerPackage, packageCachePath, token).ConfigureAwait(false))
                {
                    _logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", newerPackage, _targetFramework);
                    updatedReference = newerPackage;
                    break;
                }
            }

            return updatedReference;
        }

        private async Task<bool> DoesPackageSupportTargetFrameworkAsync(NuGetReference packageReference, string cachePath, CancellationToken token)
        {
            using var packageArchive = await _packageLoader.GetPackageArchiveAsync(packageReference, token, cachePath).ConfigureAwait(false);

            if (packageArchive is null)
            {
                return false;
            }

            var packageFrameworks = await GetTargetFrameworksAsync(packageArchive, token).ConfigureAwait(false);
            return packageFrameworks.Any(f => DefaultCompatibilityProvider.Instance.IsCompatible(_targetFramework, f));
        }

        private async Task<IEnumerable<NuGetFramework>> GetTargetFrameworksAsync(PackageArchiveReader packageArchive, CancellationToken token)
        {
            var frameworksNames = new List<NuGetFramework>();

            // Add any target framework there are libraries for
            var libraries = await packageArchive.GetLibItemsAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(libraries.Select(l => l.TargetFramework));

            // Add any target framework there are dependencies for
            var dependencies = await packageArchive.GetPackageDependenciesAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(dependencies.Select(d => d.TargetFramework));

            // Add any target framework there are reference assemblies for
            var refs = await packageArchive.GetReferenceItemsAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(refs.Select(r => r.TargetFramework));

            var ret = frameworksNames.Distinct();
            _logger.LogDebug("Found target frameworks for package {NuGetPackage}: {TargetFrameworks}", (await packageArchive.GetIdentityAsync(token).ConfigureAwait(false)).ToString(), ret);
            return ret;
        }
    }
}

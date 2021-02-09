using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UpgradeAssistant;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;

namespace AspNetMigrator.PackageUpdater.Analyzers
{
    public class TargetCompatibilityReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<TargetCompatibilityReferenceAnalyzer> _logger;

        public string Name => "Target compatibility reference analyzer";

        public TargetCompatibilityReferenceAnalyzer(IPackageLoader packageLoader, ILogger<TargetCompatibilityReferenceAnalyzer> logger)
        {
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PackageAnalysisState> AnalyzeAsync(IEnumerable<NuGetReference> references, PackageAnalysisState state, CancellationToken token)
        {
            if (references is null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var targetFramework = NuGetFramework.Parse(state.TargetTFM.Name);

            foreach (var packageReference in references.Where(r => !state.PackagesToRemove.Contains(r)))
            {
                // If the package doesn't target the right framework but a newer version does, mark it for removal and the newer version for addition
                if (await DoesPackageSupportTargetFrameworkAsync(packageReference, state.PackageCachePath!, targetFramework, token).ConfigureAwait(false))
                {
                    _logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", packageReference, targetFramework);
                    continue;
                }
                else
                {
                    // If the package won't work on the target Framework, check newer versions of the package
                    var updatedReference = await GetUpdatedPackageVersionAsync(packageReference, state.PackageCachePath!, targetFramework, token).ConfigureAwait(false);
                    if (updatedReference == null)
                    {
                        _logger.LogWarning("No version of {PackageName} found that supports {TargetFramework}; leaving unchanged", packageReference.Name, targetFramework);
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

        private async Task<NuGetReference?> GetUpdatedPackageVersionAsync(NuGetReference packageReference, string packageCachePath, NuGetFramework targetFramework, CancellationToken token)
        {
            var latestMinorVersions = await _packageLoader.GetNewerVersionsAsync(packageReference.Name, new NuGetVersion(packageReference.GetNuGetVersion()), true, token).ConfigureAwait(false);
            NuGetReference? updatedReference = null;
            foreach (var newerPackage in latestMinorVersions.Select(v => new NuGetReference(packageReference.Name, v.ToString())))
            {
                if (await DoesPackageSupportTargetFrameworkAsync(newerPackage, packageCachePath, targetFramework, token).ConfigureAwait(false))
                {
                    _logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", newerPackage, targetFramework);
                    updatedReference = newerPackage;
                    break;
                }
            }

            return updatedReference;
        }

        private async Task<bool> DoesPackageSupportTargetFrameworkAsync(NuGetReference packageReference, string cachePath, NuGetFramework targetFramework, CancellationToken token)
        {
            using var packageArchive = await _packageLoader.GetPackageArchiveAsync(packageReference, token, cachePath).ConfigureAwait(false);

            if (packageArchive is null)
            {
                return false;
            }

            var packageFrameworks = await GetTargetFrameworksAsync(packageArchive, token).ConfigureAwait(false);
            return packageFrameworks.Any(f => DefaultCompatibilityProvider.Instance.IsCompatible(targetFramework, f));
        }

        private async Task<IEnumerable<NuGetFramework>> GetTargetFrameworksAsync(PackageArchiveReader packageArchive, CancellationToken token)
        {
            var frameworksNames = new List<NuGetFramework>();

            // Add any target framework there are libraries for
            var libraries = await packageArchive.GetLibItemsAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(libraries.Select(l => l.TargetFramework));

            // Add any specific target framework there are dependencies for (do not add any, since
            // an 'any' dependency just means that does not necessarilly mean this package can work
            // with any target)
            var dependencies = await packageArchive.GetPackageDependenciesAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(dependencies.Select(d => d.TargetFramework).Where(f => !f.IsAny));

            // Add any target framework there are reference assemblies for
            var refs = await packageArchive.GetReferenceItemsAsync(token).ConfigureAwait(false);
            frameworksNames.AddRange(refs.Select(r => r.TargetFramework));

            // If no frameworks are referenced, then assume the package works everywhere (it is likely
            // a package of analyzers or some other tooling)
            if (!frameworksNames.Any())
            {
                frameworksNames.Add(NuGetFramework.AnyFramework);
            }

            var ret = frameworksNames.Distinct();
            _logger.LogDebug("Found target frameworks for package {NuGetPackage}: {TargetFrameworks}", (await packageArchive.GetIdentityAsync(token).ConfigureAwait(false)).ToString(), ret);
            return ret;
        }
    }
}

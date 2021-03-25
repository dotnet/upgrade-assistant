// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class TargetCompatibilityReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private readonly IPackageLoader _packageLoader;
        private readonly ITargetTFMSelector _tfmSelector;
        private readonly ILogger<TargetCompatibilityReferenceAnalyzer> _logger;

        public string Name => "Target compatibility reference analyzer";

        public TargetCompatibilityReferenceAnalyzer(IPackageLoader packageLoader, ITargetTFMSelector tfmSelector, ILogger<TargetCompatibilityReferenceAnalyzer> logger)
        {
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _tfmSelector = tfmSelector ?? throw new ArgumentNullException(nameof(tfmSelector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PackageAnalysisState> AnalyzeAsync(IProject project, PackageAnalysisState state, CancellationToken token)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var currentTFM = project.Required().TargetFrameworks;

            foreach (var packageReference in project.Required().PackageReferences.Where(r => !state.PackagesToRemove.Contains(r)))
            {
                // If the package doesn't target the right framework but a newer version does, mark it for removal and the newer version for addition
                if (await _packageLoader.DoesPackageSupportTargetFrameworkAsync(packageReference, state.PackageCachePath!, currentTFM, token).ConfigureAwait(false))
                {
                    _logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", packageReference, currentTFM);
                    continue;
                }
                else
                {
                    // If the package won't work on the target Framework, check newer versions of the package
                    var updatedReference = await GetUpdatedPackageVersionAsync(packageReference, state.PackageCachePath!, currentTFM, token).ConfigureAwait(false);
                    if (updatedReference == null)
                    {
                        _logger.LogWarning("No version of {PackageName} found that supports {TargetFramework}; leaving unchanged", packageReference.Name, currentTFM);
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

        private async Task<NuGetReference?> GetUpdatedPackageVersionAsync(NuGetReference packageReference, string packageCachePath, IEnumerable<TargetFrameworkMoniker> targetFramework, CancellationToken token)
        {
            var latestMinorVersions = await _packageLoader.GetNewerVersionsAsync(packageReference, true, token).ConfigureAwait(false);

            foreach (var newerPackage in latestMinorVersions)
            {
                if (await _packageLoader.DoesPackageSupportTargetFrameworkAsync(newerPackage, packageCachePath, targetFramework, token).ConfigureAwait(false))
                {
                    _logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", newerPackage, targetFramework);
                    return newerPackage;
                }
            }

            return null;
        }
    }
}

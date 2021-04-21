﻿// Licensed to the .NET Foundation under one or more agreements.
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
        private readonly IVersionComparer _comparer;
        private readonly ILogger<TargetCompatibilityReferenceAnalyzer> _logger;

        public string Name => "Target compatibility reference analyzer";

        public TargetCompatibilityReferenceAnalyzer(
            IPackageLoader packageLoader,
            ITargetTFMSelector tfmSelector,
            IVersionComparer comparer,
            ILogger<TargetCompatibilityReferenceAnalyzer> logger)
        {
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _tfmSelector = tfmSelector ?? throw new ArgumentNullException(nameof(tfmSelector));
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PackageAnalysisState> AnalyzeAsync(IProject project, PackageAnalysisState state, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var references = await project.GetNuGetReferencesAsync(token).ConfigureAwait(false);

            foreach (var packageReference in references.PackageReferences.Where(r => !state.PackagesToRemove.Contains(r)))
            {
                // If the package doesn't target the right framework but a newer version does, mark it for removal and the newer version for addition
                if (await _packageLoader.DoesPackageSupportTargetFrameworksAsync(packageReference, project.TargetFrameworks, token).ConfigureAwait(false))
                {
                    _logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", packageReference, project.TargetFrameworks);
                    continue;
                }
                else
                {
                    // If the package won't work on the target Framework, check newer versions of the package
                    var updatedReference = await GetUpdatedPackageVersionAsync(packageReference, project.TargetFrameworks, token).ConfigureAwait(false);
                    if (updatedReference == null)
                    {
                        _logger.LogWarning("No version of {PackageName} found that supports {TargetFramework}; leaving unchanged", packageReference.Name, project.TargetFrameworks);
                    }
                    else
                    {
                        _logger.LogInformation("Marking package {NuGetPackage} for removal because it doesn't support the target framework but a newer version ({Version}) does", packageReference, updatedReference.Version);
                        var isMajorChange = _comparer.IsMajorChange(updatedReference.Version, packageReference.Version);

                        if (isMajorChange)
                        {
                            _logger.LogWarning("Package {NuGetPackage} has been upgraded across major versions ({OldVersion} -> {NewVersion}) which may introduce breaking changes", packageReference.Name, packageReference.Version, updatedReference.Version);
                            state.PossibleBreakingChangeRecommended = true;
                        }

                        if (updatedReference.IsPrerelease)
                        {
                            _logger.LogWarning("Package {NuGetPackage} has been upgraded to a prerelease version ({NewVersion}) because no released version supports target(s) {TFM}", packageReference.Name, updatedReference.Version, string.Join(", ", project.TargetFrameworks));
                        }

                        state.PackagesToRemove.Add(packageReference);
                        state.PackagesToAdd.Add(updatedReference);
                    }
                }
            }

            return state;
        }

        private async Task<NuGetReference?> GetUpdatedPackageVersionAsync(NuGetReference packageReference, IEnumerable<TargetFrameworkMoniker> targetFramework, CancellationToken token)
        {
            var latestMinorVersions = await _packageLoader.GetNewerVersionsAsync(packageReference, true, token).ConfigureAwait(false);
            NuGetReference? prereleaseCandidate = null;

            foreach (var newerPackage in latestMinorVersions)
            {
                if (await _packageLoader.DoesPackageSupportTargetFrameworksAsync(newerPackage, targetFramework, token).ConfigureAwait(false))
                {
                    _logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", newerPackage, targetFramework);

                    // Only return a pre-release version if it's the only newer major version that supports the necessary TFM
                    if (newerPackage.IsPrerelease)
                    {
                        if (prereleaseCandidate is null)
                        {
                            prereleaseCandidate = newerPackage;
                        }
                    }
                    else
                    {
                        return newerPackage;
                    }
                }
            }

            return prereleaseCandidate;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class TargetCompatibilityReferenceAnalyzer : IDependencyAnalyzer
    {
        private readonly IPackageLoader _packageLoader;
        private readonly IVersionComparer _comparer;
        private readonly ILogger<TargetCompatibilityReferenceAnalyzer> _logger;

        public string Name => "Target compatibility reference analyzer";

        public TargetCompatibilityReferenceAnalyzer(
            IPackageLoader packageLoader,
            IVersionComparer comparer,
            ILogger<TargetCompatibilityReferenceAnalyzer> logger)
        {
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AnalyzeAsync(IProject project, IDependencyAnalysisState state, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            foreach (var packageReference in state.Packages)
            {
                // If the package doesn't target the right framework but a newer version does, mark it for removal and the newer version for addition
                if (await _packageLoader.DoesPackageSupportTargetFrameworksAsync(packageReference, state.TargetFrameworks, token).ConfigureAwait(false))
                {
                    _logger.LogDebug("Package {NuGetPackage} will work on {TargetFramework}", packageReference, state.TargetFrameworks);
                    continue;
                }
                else
                {
                    // If the package won't work on the target Framework, check newer versions of the package
                    var newerVersions = await _packageLoader.GetNewerVersionsAsync(packageReference, state.TargetFrameworks, new() { LatestMinorAndBuildOnly = true }, token).ConfigureAwait(false);
                    var updatedReference = newerVersions.FirstOrDefault();

                    if (updatedReference == null)
                    {
                        _logger.LogWarning("No version of {PackageName} found that supports {TargetFramework}; leaving unchanged", packageReference.Name, state.TargetFrameworks);
                    }
                    else
                    {
                        _logger.LogInformation("Marking package {NuGetPackage} for removal because it doesn't support the target framework but a newer version ({Version}) does", packageReference, updatedReference.Version);
                        var isMajorChange = _comparer.IsMajorChange(updatedReference.Version, packageReference.Version);

                        if (isMajorChange)
                        {
                            _logger.LogWarning("Package {NuGetPackage} has been upgraded across major versions ({OldVersion} -> {NewVersion}) which may introduce breaking changes", packageReference.Name, packageReference.Version, updatedReference.Version);
                        }

                        if (updatedReference.IsPrerelease)
                        {
                            _logger.LogWarning("Package {NuGetPackage} has been upgraded to a prerelease version ({NewVersion}) because no released version supports target(s) {TFM}", packageReference.Name, updatedReference.Version, string.Join(", ", state.TargetFrameworks));
                        }

                        state.Packages.Remove(packageReference);
                        state.Packages.Add(updatedReference, isMajorChange ? BuildBreakRisk.Medium : BuildBreakRisk.Low);
                    }
                }
            }
        }
    }
}

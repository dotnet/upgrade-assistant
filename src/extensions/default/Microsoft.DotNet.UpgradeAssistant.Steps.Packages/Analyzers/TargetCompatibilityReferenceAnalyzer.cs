// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

            // Making a copy of state.Packages collection to avoid modifying a collection that is being enumerated.
            var packages = state.Packages.ToList();

            foreach (var packageReference in packages)
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
                    var newerVersions = _packageLoader.GetNewerVersionsAsync(packageReference, state.TargetFrameworks, new() { LatestMinorAndBuildOnly = true }, token);
                    var updatedReference = await newerVersions.FirstOrDefaultAsync().ConfigureAwait(false);
                    var details = new List<string>();

                    if (updatedReference == null)
                    {
                        _logger.LogWarning("No version of {PackageName} found that supports {TargetFramework}; leaving unchanged", packageReference.Name, state.TargetFrameworks);
                    }
                    else
                    {
                        var logMessage = SR.Format("Package {0} does not support the target(s) {1} but a newer version ({2}) does.", packageReference, string.Join(", ", state.TargetFrameworks), updatedReference.Version);
                        _logger.LogInformation(logMessage);
                        var isMajorChange = _comparer.IsMajorChange(updatedReference.Version, packageReference.Version);

                        if (isMajorChange)
                        {
                            var logString = SR.Format("Package {0} needs to be upgraded across major versions ({1} -> {2}) which may introduce breaking changes.", packageReference.Name, packageReference.Version, updatedReference.Version);
                            details.Add($"{logMessage}  {logString}");
                            _logger.LogWarning(logString);
                        }

                        if (updatedReference.IsPrerelease)
                        {
                            var logString = SR.Format("Package {0} needs to be upgraded to a prerelease version ({1}) because no released version supports target(s) {2}", packageReference.Name, updatedReference.Version, string.Join(", ", state.TargetFrameworks));
                            details.Add($"{logMessage}  {logString}");
                            _logger.LogWarning(logString);
                        }

                        if (!isMajorChange && !updatedReference.IsPrerelease)
                        {
                            details.Add($"{logMessage}  {SR.Format("Package {0} needs to be upgraded from {1} to {2}.", packageReference.Name, packageReference.Version, updatedReference.Version)}");
                        }

                        state.Packages.Remove(packageReference, new OperationDetails() { Risk = BuildBreakRisk.None, Details = details });
                        state.Packages.Add(updatedReference, new OperationDetails() { Risk = isMajorChange ? BuildBreakRisk.Medium : BuildBreakRisk.Low, Details = details });
                    }
                }
            }
        }
    }
}

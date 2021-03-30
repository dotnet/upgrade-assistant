// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class WindowsCompatReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private const string PackageName = "Microsoft.Windows.Compatibility";

        private readonly ILogger<WindowsCompatReferenceAnalyzer> _logger;
        private readonly IPackageLoader _loader;

        public WindowsCompatReferenceAnalyzer(ILogger<WindowsCompatReferenceAnalyzer> logger, IPackageLoader loader)
        {
            _logger = logger;
            _loader = loader;
        }

        public string Name => "Windows Compatibility Pack Analyzer";

        /// <summary>
        /// Limits the step from being applied to the wrong project types.
        /// </summary>
        /// <param name="project">The project whose NuGet package references should be analyzed.</param>
        /// <param name="token">The token used to gracefully cancel this request.</param>
        /// <returns>True if the project targets windows and the package is not already transitively available.</returns>
        public Task<bool> IsApplicableAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (!project.TargetFrameworks.Any(tfm => tfm.IsWindows))
            {
                return Task.FromResult(false);
            }

            if (project.IsTransitivelyAvailable(PackageName))
            {
                _logger.LogDebug("{PackageName} already referenced transitively", PackageName);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
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

            if (!await IsApplicableAsync(project, token).ConfigureAwait(false))
            {
                return state;
            }

            var latestVersion = await _loader.GetLatestVersionAsync(PackageName, false, null, token).ConfigureAwait(false);

            if (latestVersion is null)
            {
                _logger.LogWarning("Could not find {PackageName}", latestVersion);
                return state;
            }

            if (project.TryGetPackageByName(PackageName, out var existing))
            {
                var version = existing.GetNuGetVersion();

                if (version >= latestVersion.GetNuGetVersion())
                {
                    return state;
                }

                state.PackagesToRemove.Add(existing);
            }

            _logger.LogInformation("Adding {PackageName} {Version}", PackageName, latestVersion.Version);

            state.PackagesToAdd.Add(new NuGetReference(PackageName, latestVersion.Version));

            return state;
        }
    }
}

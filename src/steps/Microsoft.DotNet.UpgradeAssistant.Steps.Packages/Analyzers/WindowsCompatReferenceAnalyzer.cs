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

        public async Task<PackageAnalysisState> AnalyzeAsync(IProject project, PackageAnalysisState state, CancellationToken token)
        {
            if (!project.Required().TargetFrameworks.Any(tfm => tfm.IsWindows))
            {
                return state;
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (project.IsTransitivelyAvailable(PackageName))
            {
                _logger.LogDebug("{PackageName} already referenced transitively", PackageName);
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

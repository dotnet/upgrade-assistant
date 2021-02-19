// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    internal class WindowsCompatReferenceAnalyzer : IPackageReferencesAnalyzer
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

        public async Task<PackageAnalysisState> AnalyzeAsync(PackageCollection references, PackageAnalysisState state, CancellationToken token)
        {
            if (!state.CurrentTFM.IsWindows)
            {
                return state;
            }

            if (state.IsTransitivelyAvailable(PackageName))
            {
                _logger.LogDebug("{PackageName} already referenced transitively", PackageName);
                return state;
            }

            var latestVersion = await _loader.GetLatestVersionAsync(PackageName, false, null, token);

            if (latestVersion is null)
            {
                _logger.LogWarning("Could not find {PackageName}", latestVersion);
                return state;
            }

            if (references.TryGetPackageByName(PackageName, out var existing))
            {
                var version = existing.GetNuGetVersion();

                if (version >= latestVersion)
                {
                    return state;
                }

                state.PackagesToRemove.Add(existing);
            }

            var versionToAdd = latestVersion.ToNormalizedString();
            _logger.LogInformation("Adding {PackageName} {Version}", PackageName, versionToAdd);

            state.PackagesToAdd.Add(new NuGetReference(PackageName, versionToAdd));

            return state;
        }
    }
}

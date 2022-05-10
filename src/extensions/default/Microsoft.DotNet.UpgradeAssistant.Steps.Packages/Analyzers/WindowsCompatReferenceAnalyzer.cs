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
    public class WindowsCompatReferenceAnalyzer : IDependencyAnalyzer
    {
        private const string PackageName = "Microsoft.Windows.Compatibility";
        private readonly ITransitiveDependencyIdentifier _transitiveIdentifier;
        private readonly IPackageLoader _loader;
        private readonly IVersionComparer _comparer;
        private readonly ILogger<WindowsCompatReferenceAnalyzer> _logger;

        public WindowsCompatReferenceAnalyzer(
            ITransitiveDependencyIdentifier transitiveIdentifier,
            IPackageLoader loader,
            IVersionComparer comparer,
            ILogger<WindowsCompatReferenceAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transitiveIdentifier = transitiveIdentifier ?? throw new ArgumentNullException(nameof(transitiveIdentifier));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        public string Name => "Windows Compatibility Pack Analyzer";

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

            if (!state.TargetFrameworks.Any(tfm => tfm.IsWindows))
            {
                return;
            }

            if (await _transitiveIdentifier.IsTransitiveDependencyAsync(PackageName, project, token).ConfigureAwait(false))
            {
                _logger.LogDebug("{PackageName} already referenced transitively", PackageName);
                return;
            }

            var latestVersion = await _loader.GetLatestVersionAsync(PackageName, state.TargetFrameworks, new(), token).ConfigureAwait(false);

            if (latestVersion is null)
            {
                _logger.LogWarning("Could not find {PackageName}", latestVersion);
                return;
            }

            if (project.NuGetReferences.TryGetPackageByName(PackageName, out var existing))
            {
                if (_comparer.Compare(existing.Version, latestVersion.Version) >= 0)
                {
                    return;
                }

                state.Packages.Remove(existing, new OperationDetails());
            }

            var logMessage = SR.Format("Adding {0} {1} helps with speeding up the upgrade process for Windows-based APIs", PackageName, latestVersion.Version);
            _logger.LogInformation(logMessage);

            state.Packages.Add(new NuGetReference(PackageName, latestVersion.Version), new OperationDetails { Details = new[] { logMessage } });
        }
    }
}

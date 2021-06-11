// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class UpgradeAssistantReferenceAnalyzer : IDependencyAnalyzer
    {
        private const string AnalyzerPackageName = "Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers";

        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<UpgradeAssistantReferenceAnalyzer> _logger;

        public string Name => "Upgrade assistant reference analyzer";

        public UpgradeAssistantReferenceAnalyzer(IOptions<PackageUpdaterOptions> updaterOptions, IPackageLoader packageLoader, ILogger<UpgradeAssistantReferenceAnalyzer> logger)
        {
            if (updaterOptions is null)
            {
                throw new ArgumentNullException(nameof(updaterOptions));
            }

            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
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

            // If the project doesn't include a reference to the analyzer package, mark it for addition
            if (!state.Packages.Any(r => AnalyzerPackageName.Equals(r.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var analyzerPackage = await _packageLoader.GetLatestVersionAsync(AnalyzerPackageName, state.TargetFrameworks, true, token).ConfigureAwait(false);

                if (analyzerPackage is not null)
                {
                    _logger.LogInformation("Reference to .NET Upgrade Assistant analyzer package ({AnalyzerPackageName}, version {AnalyzerPackageVersion}) needs added", AnalyzerPackageName, analyzerPackage.Version);
                    state.Packages.Add(analyzerPackage with { PrivateAssets = "all" });
                }
                else
                {
                    _logger.LogWarning(".NET Upgrade Assistant analyzer NuGet package reference cannot be added because the package cannot be found");
                }
            }
            else
            {
                _logger.LogDebug("Reference to .NET Upgrade Assistant analyzer package ({AnalyzerPackageName}) already exists", AnalyzerPackageName);
            }
        }
    }
}

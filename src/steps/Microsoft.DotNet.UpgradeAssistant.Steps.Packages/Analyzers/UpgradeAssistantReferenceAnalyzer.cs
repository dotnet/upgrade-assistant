// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class UpgradeAssistantReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private const string AnalyzerPackageName = "Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers";

        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<UpgradeAssistantReferenceAnalyzer> _logger;
        private readonly string? _analyzerPackageVersion;

        public string Name => "Upgrade assistant reference analyzer";

        public UpgradeAssistantReferenceAnalyzer(IOptions<PackageUpdaterOptions> updaterOptions, IPackageLoader packageLoader, ILogger<UpgradeAssistantReferenceAnalyzer> logger)
        {
            if (updaterOptions is null)
            {
                throw new ArgumentNullException(nameof(updaterOptions));
            }

            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _analyzerPackageVersion = updaterOptions.Value.UpgradeAnalyzersPackageVersion;
        }

        public async Task<PackageAnalysisState> AnalyzeAsync(IProject project, PackageAnalysisState state, CancellationToken token)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var packageReferences = project.Required().NuGetReferences.PackageReferences.Where(r => !state.PackagesToRemove.Contains(r));

            // If the project doesn't include a reference to the analyzer package, mark it for addition
            if (!packageReferences.Any(r => AnalyzerPackageName.Equals(r.Name, StringComparison.OrdinalIgnoreCase)))
            {
                // Use the analyzer package version from configuration if specified, otherwise get the latest version.
                // When looking for the latest analyzer version, use the analyzer package source from configuration
                // if one is specified, otherwise just use the package sources from the project being analyzed.
                var analyzerPackage = _analyzerPackageVersion is not null
                    ? new NuGetReference(AnalyzerPackageName, _analyzerPackageVersion)
                    : await _packageLoader.GetLatestVersionAsync(AnalyzerPackageName, true, null, token).ConfigureAwait(false);

                if (analyzerPackage is not null)
                {
                    _logger.LogInformation("Reference to .NET Upgrade Assistant analyzer package ({AnalyzerPackageName}, version {AnalyzerPackageVersion}) needs added", AnalyzerPackageName, analyzerPackage.Version);
                    state.PackagesToAdd.Add(analyzerPackage with { PrivateAssets = "all" });
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

            return state;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace AspNetMigrator.PackageUpdater.Analyzers
{
    public class UpgradeAssistantReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private const string AnalyzerPackageName = "AspNetMigrator.Analyzers";
        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<UpgradeAssistantReferenceAnalyzer> _logger;
        private readonly string? _analyzerPackageSource;
        private readonly string? _analyzerPackageVersion;

        public string Name => "Duplicate reference analyzer";

        public UpgradeAssistantReferenceAnalyzer(IOptions<PackageUpdaterOptions> updaterOptions, IPackageLoader packageLoader, ILogger<UpgradeAssistantReferenceAnalyzer> logger)
        {
            if (updaterOptions is null)
            {
                throw new ArgumentNullException(nameof(updaterOptions));
            }

            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _analyzerPackageSource = updaterOptions.Value.MigrationAnalyzersPackageSource;
            _analyzerPackageVersion = updaterOptions.Value.MigrationAnalyzersPackageVersion;
        }

        public async Task<PackageAnalysisState> AnalyzeAsync(IMigrationContext context, IEnumerable<NuGetReference> references, PackageAnalysisState? state, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (references is null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            if (state is null)
            {
                state = new PackageAnalysisState(context);
            }

            var packageReferences = references.Where(r => !state.PackagesToRemove.Contains(r));

            // If the project doesn't include a reference to the analyzer package, mark it for addition
            if (!packageReferences.Any(r => AnalyzerPackageName.Equals(r.Name, StringComparison.OrdinalIgnoreCase)))
            {
                // Use the analyzer package version from configuration if specified, otherwise get the latest version.
                // When looking for the latest analyzer version, use the analyzer package source from configuration
                // if one is specified, otherwise just use the package sources from the project being analyzed.
                var analyzerPackageVersion = _analyzerPackageVersion is not null
                    ? NuGetVersion.Parse(_analyzerPackageVersion)
                    : await _packageLoader.GetLatestVersionAsync(AnalyzerPackageName, true, _analyzerPackageSource is null ? null : new[] { _analyzerPackageSource }, token).ConfigureAwait(false);
                if (analyzerPackageVersion is not null)
                {
                    _logger.LogInformation("Reference to .NET Upgrade Assistant analyzer package ({AnalyzerPackageName}, version {AnalyzerPackageVersion}) needs added", AnalyzerPackageName, analyzerPackageVersion);
                    state.PackagesToAdd.Add(new NuGetReference(AnalyzerPackageName, analyzerPackageVersion.ToString()));
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

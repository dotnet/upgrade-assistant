using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UpgradeAssistant;

namespace AspNetMigrator.PackageUpdater.Analyzers
{
    public class PackageMapReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private readonly ILogger<PackageMapReferenceAnalyzer> _logger;
        private readonly PackageMapProvider _packageMapProvider;

        public string Name => "Package map reference analyzer";

        public PackageMapReferenceAnalyzer(PackageMapProvider packageMapProvider, ILogger<PackageMapReferenceAnalyzer> logger)
        {
            _packageMapProvider = packageMapProvider ?? throw new ArgumentNullException(nameof(packageMapProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PackageAnalysisState> AnalyzeAsync(IEnumerable<NuGetReference> references, PackageAnalysisState state, CancellationToken token)
        {
            if (references is null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            // Get package maps as an array here so that they're only loaded once (as opposed to each iteration through the loop)
            var packageMaps = await _packageMapProvider.GetPackageMapsAsync(token).ToArrayAsync(token).ConfigureAwait(false);

            foreach (var packageReference in references.Where(r => !state.PackagesToRemove.Contains(r)))
            {
                foreach (var map in packageMaps.Where(m => m.ContainsReference(packageReference.Name, packageReference.Version)))
                {
                    if (map != null)
                    {
                        state.PossibleBreakingChangeRecommended = true;
                        _logger.LogInformation("Marking package {PackageName} for removal based on package mapping configuration {PackageMapSet}", packageReference.Name, map.PackageSetName);
                        state.PackagesToRemove.Add(packageReference);
                        foreach (var newPackage in map.NetCorePackages)
                        {
                            state.PackagesToAdd.Add(newPackage);
                        }
                    }
                }
            }

            return state;
        }
    }
}

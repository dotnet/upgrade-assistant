using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.PackageUpdater.Analyzers
{
    public class DuplicateReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private readonly ILogger<DuplicateReferenceAnalyzer> _logger;

        public string Name => "Duplicate reference analyzer";

        public DuplicateReferenceAnalyzer(ILogger<DuplicateReferenceAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<PackageAnalysisState> AnalyzeAsync(IEnumerable<NuGetReference> references, PackageAnalysisState state, CancellationToken token)
        {
            if (references is null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            foreach (var packageReference in references.Where(r => !state.PackagesToRemove.Contains(r)))
            {
                // If the package is referenced more than once (bizarrely, this happens one of our test inputs), only keep the highest version
                var highestVersion = references
                    .Where(r => r.Name.Equals(packageReference.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.GetNuGetVersion())
                    .Max();
                if (highestVersion > packageReference.GetNuGetVersion())
                {
                    _logger.LogInformation("Marking package {NuGetPackage} for removal because it is referenced elsewhere in the project with a higher version", packageReference);
                    state.PackagesToRemove.Add(packageReference);
                    continue;
                }
            }

            return Task.FromResult(state);
        }
    }
}

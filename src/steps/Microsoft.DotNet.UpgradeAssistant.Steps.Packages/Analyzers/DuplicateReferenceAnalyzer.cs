﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class DuplicateReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private readonly ILogger<DuplicateReferenceAnalyzer> _logger;

        public string Name => "Duplicate reference analyzer";

        public DuplicateReferenceAnalyzer(ILogger<DuplicateReferenceAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<PackageAnalysisState> AnalyzeAsync(PackageCollection references, PackageAnalysisState state, CancellationToken token)
        {
            if (references is null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            // If the package is referenced more than once (bizarrely, this happens one of our test inputs), only keep the highest version
            foreach (var duplicates in references.Packages.Where(g => g.Count() > 1))
            {
                var highestVersion = duplicates.Select(p => p.GetNuGetVersion()).Max();

                foreach (var package in duplicates.Where(p => p.GetNuGetVersion() != highestVersion))
                {
                    _logger.LogInformation("Marking package {NuGetPackage} for removal because it is referenced elsewhere in the project with a higher version", package);
                    state.PackagesToRemove.Add(package);
                }
            }

            return Task.FromResult(state);
        }
    }
}

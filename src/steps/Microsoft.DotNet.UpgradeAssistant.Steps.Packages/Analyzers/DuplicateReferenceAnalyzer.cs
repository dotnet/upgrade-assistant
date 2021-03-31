// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class DuplicateReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private readonly IVersionComparer _comparer;
        private readonly ILogger<DuplicateReferenceAnalyzer> _logger;

        public string Name => "Duplicate reference analyzer";

        public DuplicateReferenceAnalyzer(IVersionComparer comparer, ILogger<DuplicateReferenceAnalyzer> logger)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<PackageAnalysisState> AnalyzeAsync(IProject project, PackageAnalysisState state, CancellationToken token)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            // If the package is referenced more than once (bizarrely, this happens one of our test inputs), only keep the highest version
            var packages = project.Required().NuGetReferences.PackageReferences.ToLookup(p => p.Name);
            foreach (var duplicates in packages.Where(g => g.Count() > 1))
            {
                var highestVersion = duplicates.OrderByDescending(p => p, _comparer).First();

                foreach (var package in duplicates.Where(p => p != highestVersion))
                {
                    _logger.LogInformation("Marking package {NuGetPackage} for removal because it is referenced elsewhere in the project with a higher version", package);
                    state.PackagesToRemove.Add(package);
                }
            }

            return Task.FromResult(state);
        }
    }
}

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
    public class DuplicateReferenceAnalyzer : IDependencyAnalyzer
    {
        private readonly IVersionComparer _comparer;
        private readonly ILogger<DuplicateReferenceAnalyzer> _logger;

        public string Name => "Duplicate reference analyzer";

        public DuplicateReferenceAnalyzer(IVersionComparer comparer, ILogger<DuplicateReferenceAnalyzer> logger)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task AnalyzeAsync(IProject project, IDependencyAnalysisState state, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            // If the package is referenced more than once (bizarrely, this happens one of our test inputs), only keep the highest version
            var packages = project.NuGetReferences
                .PackageReferences
                .ToLookup(p => p.Name)
                .Where(g => g.Count() > 1);

            foreach (var duplicates in packages)
            {
                var highestVersion = duplicates.OrderByDescending(p => p.Version, _comparer).First();

                foreach (var package in duplicates.Where(p => p != highestVersion))
                {
                    var logMessage = SR.Format("Marking package {0} for removal because it is referenced elsewhere in the project with a higher version", package);
                    _logger.LogInformation(logMessage);
                    state.Packages.Remove(package, new OperationDetails { Details = new[] { logMessage } });
                }
            }

            return Task.CompletedTask;
        }
    }
}

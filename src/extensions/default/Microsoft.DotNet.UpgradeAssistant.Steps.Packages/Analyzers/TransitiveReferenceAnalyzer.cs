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
    [Order(int.MaxValue)]
    public class TransitiveReferenceAnalyzer : IDependencyAnalyzer
    {
        private readonly ILogger<TransitiveReferenceAnalyzer> _logger;

        public string Name => "Transitive reference analyzer";

        public TransitiveReferenceAnalyzer(ILogger<TransitiveReferenceAnalyzer> logger)
        {
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

            bool completed;
            var count = 0;

            // The maximum iterations should be the number of packages. This is the worst case when all the packages are brought in by referencing a single package.
            var maxIterations = state.Packages.Count();

            do
            {
                if (count++ > maxIterations)
                {
                    _logger.LogError("Maximum iterations ({MaxIterations}) was hit attempting to reduce transitive dependencies", maxIterations);
                    return;
                }

                completed = true;
                var currentSet = state.Packages.Except(state.Packages.Deletions.Select(d => d.Item)).ToList();

                // If the package is referenced transitively, mark for removal
                foreach (var packageReference in currentSet)
                {
                    if (await project.NuGetReferences.IsTransitiveDependencyAsync(packageReference, currentSet, token).ConfigureAwait(false))
                    {
                        state.Packages.Remove(packageReference, new OperationDetails { Details = new[] { "Unnecessary transitive dependency" } });
                        completed = false;
                    }
                }
            }
            while (!completed);
        }
    }
}

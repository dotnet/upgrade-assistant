// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
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

            var references = await project.GetNuGetReferencesAsync(token).ConfigureAwait(false);

            // If the package is referenced transitively, mark for removal
            foreach (var packageReference in state.Packages)
            {
                if (references.IsTransitiveDependency(packageReference))
                {
                    _logger.LogInformation("Marking package {PackageName} for removal because it appears to be a transitive dependency", packageReference.Name);
                    state.Packages.Remove(packageReference);
                }
            }
        }
    }
}

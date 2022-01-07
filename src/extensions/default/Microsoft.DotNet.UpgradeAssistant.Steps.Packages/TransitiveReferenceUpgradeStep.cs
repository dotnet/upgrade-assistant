// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class TransitiveReferenceUpgradeStep : PackageUpdaterStep
    {
        public TransitiveReferenceUpgradeStep(IDependencyAnalyzerRunner runner, ILogger<TransitiveReferenceUpgradeStep> logger)
            : base(new[] { new TransitiveReferenceAnalyzer(logger) }, runner, logger)
        {
        }

        public override string Id => typeof(TransitiveReferenceUpgradeStep).FullName;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.PackageUpdaterStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        private class TransitiveReferenceAnalyzer : IDependencyAnalyzer
        {
            private readonly ILogger _logger;

            public string Name => "Transitive reference analyzer";

            public TransitiveReferenceAnalyzer(ILogger logger)
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

                // If the package is referenced transitively, mark for removal
                foreach (var packageReference in state.Packages)
                {
                    if (await project.NuGetReferences.IsTransitiveDependencyAsync(packageReference, token).ConfigureAwait(false))
                    {
                        _logger.LogInformation("Marking package {PackageName} for removal because it appears to be a transitive dependency", packageReference.Name);
                        state.Packages.Remove(packageReference, new OperationDetails());
                    }
                }
            }
        }
    }
}

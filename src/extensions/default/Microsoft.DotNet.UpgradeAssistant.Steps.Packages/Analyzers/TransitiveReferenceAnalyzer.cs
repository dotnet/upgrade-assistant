// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    [Order(int.MaxValue)]
    public class TransitiveReferenceAnalyzer : IDependencyAnalyzer
    {
        private readonly ITransitiveDependencyIdentifier _transitiveChecker;

        public TransitiveReferenceAnalyzer(ITransitiveDependencyIdentifier transitiveChecker)
        {
            _transitiveChecker = transitiveChecker ?? throw new ArgumentNullException(nameof(transitiveChecker));
        }

        public string Name => "Transitive reference analyzer";

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

            foreach (var packageReference in await _transitiveChecker.RemoveTransitiveDependenciesAsync(state.Packages, state.TargetFrameworks, token).ConfigureAwait(false))
            {
                state.Packages.Remove(packageReference, new OperationDetails { Details = new[] { "Unnecessary transitive dependency" } });
            }
        }
    }
}

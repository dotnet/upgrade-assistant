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

            // If the package is referenced transitively, mark for removal
            foreach (var packageReference in state.Packages)
            {
                if (await project.NuGetReferences.IsTransitiveDependencyAsync(packageReference, token).ConfigureAwait(false))
                {
                    state.Packages.Remove(packageReference, new OperationDetails { Details = new[] { "Unnecessary transitive dependency" } });
                }
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Dependencies
{
    public interface IDependencyAnalyzer
    {
        string Name { get; }

        /// <summary>
        /// Analyzes a project's dependencies and identify dependencies that should be removed or updated along with
        /// the breaking change risk of this action.
        /// </summary>
        /// <param name="project">The project whose dependencies should be analyzed.</param>
        /// <param name="targetframeworks">The targetframworkmonikers applicable for the project.</param>
        /// <param name="state">The current analysis state which will be updated.</param>
        /// <param name="token">The token used to gracefully cancel this request.</param>
        Task AnalyzeAsync(IProject project, IReadOnlyCollection<TargetFrameworkMoniker> targetframeworks, IDependencyAnalysisState state, CancellationToken token);
    }
}

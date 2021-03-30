// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public interface IPackageReferencesAnalyzer
    {
        /// <summary>
        /// Used to determine if a step should be applied at a specific context.
        /// </summary>
        /// <param name="project">The project whose NuGet package references should be analyzed.</param>
        /// <param name="token">The token used to gracefully cancel this request.</param>
        /// <returns>True if the analyzer is applicable to the project in its current state.</returns>
        Task<bool> IsApplicableAsync(IProject project, CancellationToken token);

        string Name { get; }

        /// <summary>
        /// Analyzes a project's package references, updating a provided analysis state object with details including
        /// which packages (if any) should be removed, which (if any) should be added, whether the proposed changes are
        /// likely to introduce breaking changes, and whether there were errors performing the analysis.
        /// </summary>
        /// <param name="project">The project whose NuGet package references should be analyzed.</param>
        /// <param name="state">The current analysis state which will be updated and returned.</param>
        /// <param name="token">The token used to gracefully cancel this request.</param>
        /// <returns>The analysis state object provided updated based on this analyzer's analysis.</returns>
        Task<PackageAnalysisState> AnalyzeAsync(IProject project, PackageAnalysisState state, CancellationToken token);
    }
}

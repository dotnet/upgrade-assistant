using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UpgradeAssistant.Steps.Packages
{
    public interface IPackageReferencesAnalyzer
    {
        string Name { get; }

        /// <summary>
        /// Analyzes an enumerable of package references, updating a provided analysis state object with details including
        /// which packages (if any) should be removed, which (if any) should be added, whether the proposed changes are
        /// likely to introduce breaking changes, and whether there were errors performing the analysis.
        /// </summary>
        /// <param name="references">The NuGet package references to analyze.</param>
        /// <param name="state">The current analysis state which will be updated and returned.</param>
        /// <returns>The analysis state object provided updated based on this analyzer's analysis.</returns>
        Task<PackageAnalysisState> AnalyzeAsync(IEnumerable<NuGetReference> references, PackageAnalysisState state, CancellationToken token);
    }
}

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace Microsoft.UpgradeAssistant.Steps.Packages.Analyzers
{
    internal class WindowsCompatReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private const string PackageName = "Microsoft.Windows.Compatibility";

        private readonly NuGetVersion _version;
        private readonly ILogger<WindowsCompatReferenceAnalyzer> _logger;

        public WindowsCompatReferenceAnalyzer(ILogger<WindowsCompatReferenceAnalyzer> logger)
        {
            _version = NuGetVersion.Parse("5.0.2");
            _logger = logger;
        }

        public string Name => "Windows Compatibility Pack Analyzer";

        public Task<PackageAnalysisState> AnalyzeAsync(PackageCollection references, PackageAnalysisState state, CancellationToken token)
        {
            if (!state.CurrentTFM.IsWindows)
            {
                return Task.FromResult(state);
            }

            if (references.TryGetPackageByName(PackageName, out var existing))
            {
                var version = existing.GetNuGetVersion();

                if (version >= _version)
                {
                    _logger.LogInformation("Already contains {PackageName} {Version}", PackageName, version);

                    return Task.FromResult(state);
                }

                state.PackagesToRemove.Add(existing);
            }

            _logger.LogInformation("Adding {PackageName} {Version}", PackageName, _version);

            state.PackagesToAdd.Add(new NuGetReference(PackageName, _version.OriginalVersion));

            return Task.FromResult(state);
        }
    }
}

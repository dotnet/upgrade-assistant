using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class AnalyzePackageStatus : IAnalyzeResultProvider
    {
        private readonly IPackageRestorer _packageRestorer;
        private readonly IEnumerable<IPackageReferencesAnalyzer> _packageAnalyzers;
        private readonly IPackageAnalyzer _packageAnalyzer;

        private PackageAnalysisState? _analysisState;

        protected ILogger Logger { get; }

        public AnalyzePackageStatus(IPackageRestorer packageRestorer,
            IEnumerable<IPackageReferencesAnalyzer> packageAnalyzers,
            ILogger<AnalyzePackageStatus> logger)
        {
            Logger = logger;
            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageAnalyzers = packageAnalyzers ?? throw new ArgumentNullException(nameof(packageAnalyzers));
            _analysisState = null;
            _packageAnalyzer = new PackageAnalyzer(_packageRestorer, _packageAnalyzers, logger);
        }

        public async Task AnalyzeAsync(AnalyzeContext analysis, CancellationToken token)
        {
            if (analysis is null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var context = analysis.UpgradeContext;
            try
            {
                if (!await _packageAnalyzer.RunPackageAnalyzersAsync(context, _analysisState, token).ConfigureAwait(false))
                {
                    Logger.LogCritical("Package Analysis Failed for: {ProjectPath}", context.CurrentProject.Required().FileInfo);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogCritical(exc, "Unexpected exception analyzing package references for: {ProjectPath}", context.CurrentProject.Required().FileInfo);

                // return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Unexpected exception analyzing package references for: {context.CurrentProject.Required().FileInfo}", BuildBreakRisk.Unknown);
            }
        }
    }
}

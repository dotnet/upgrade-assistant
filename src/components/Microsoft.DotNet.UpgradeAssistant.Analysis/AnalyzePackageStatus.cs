using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class AnalyzePackageStatus : IAnalyzeResultProvider
    {
        private readonly IPackageRestorer _packageRestorer;
        private readonly IEnumerable<IDependencyAnalyzer> _packageAnalyzers;
        private readonly IDependencyAnalyzerRunner _packageAnalyzer;

        private IDependencyAnalysisState? _analysisState;

        protected ILogger Logger { get; }

        public AnalyzePackageStatus(IPackageRestorer packageRestorer,
            IEnumerable<IDependencyAnalyzer> packageAnalyzers,
            IDependencyAnalyzerRunner packageAnalyzer,
            ILogger<AnalyzePackageStatus> logger)
        {
            Logger = logger;
            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageAnalyzers = packageAnalyzers ?? throw new ArgumentNullException(nameof(packageAnalyzers));
            _packageAnalyzer = packageAnalyzer ?? throw new ArgumentNullException(nameof(packageAnalyzer));
            _analysisState = null;
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
                _analysisState = await _packageAnalyzer.AnalyzeAsync(context, context.CurrentProject, token).ConfigureAwait(false);
                if (!_analysisState.IsValid)
                {
                    Logger.LogError($"Package analysis failed");
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogCritical(exc, "Unexpected exception analyzing package references for: {ProjectPath}", context.CurrentProject.Required().FileInfo);
            }
        }
    }
}

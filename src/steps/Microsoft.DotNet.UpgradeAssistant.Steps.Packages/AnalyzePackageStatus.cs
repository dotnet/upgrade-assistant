using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class AnalyzePackageStatus : IAnalyzeResultProvider
    {
        private const string SarifLogFilePath = "Dependencies.sarif";
        private readonly IDependencyAnalyzerRunner _packageAnalyzer;
        private readonly ITargetFrameworkSelector _tfmSelector;
        private readonly IProcessResult _processResult;
        private IDependencyAnalysisState? _analysisState;
        private IJsonSerializer _jsonSerializer;

        private ILogger Logger { get; }

        public AnalyzePackageStatus(IDependencyAnalyzerRunner packageAnalyzer,
            ITargetFrameworkSelector tfmSelector,
            ILogger<AnalyzePackageStatus> logger,
            IProcessResult processResult,
            IJsonSerializer jsonSerializer)
        {
            Logger = logger;
            _tfmSelector = tfmSelector;
            _processResult = processResult;
            _jsonSerializer = jsonSerializer;
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
            var dependencies = analysis.Dependencies;
            var projects = context.Projects.ToList();

            foreach (var project in projects)
            {
                var targetTfm = await _tfmSelector.SelectTargetFrameworkAsync(project, token).ConfigureAwait(false);
                var targetframeworks = new TargetFrameworkMoniker[]
                {
                        targetTfm
                };

                try
                {
                    _analysisState = await _packageAnalyzer.AnalyzeAsync(context, project, targetframeworks, token).ConfigureAwait(false);
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

#pragma warning disable CS8604 // Possible null reference argument.
                dependencies.Add(project.FileInfo.Name, _analysisState);
#pragma warning restore CS8604 // Possible null reference argument.
            }

            WriteResults(dependencies);
        }

        public void WriteResults(Dictionary<string, IDependencyAnalysisState> dependencies)
        {
            var writeOutput = new WriteOutput(_jsonSerializer);
            var sarifLog = WriteOutput.CreateSarifLog(_processResult.RunProcessResult("Package Dependency Analysis for", dependencies));
            writeOutput.Write(Path.Combine(Directory.GetCurrentDirectory(), SarifLogFilePath), sarifLog);
        }
    }
}

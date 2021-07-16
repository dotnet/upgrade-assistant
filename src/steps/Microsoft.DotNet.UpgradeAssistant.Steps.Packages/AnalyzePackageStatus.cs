using System;
using System.Collections.Generic;
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
        private readonly IDependencyAnalyzerRunner _packageAnalyzer;
        private readonly ITargetFrameworkSelector _tfmSelector;
        private IDependencyAnalysisState? _analysisState;

        private ILogger Logger { get; }

        public string AnalysisTypeName => "Dependency Analysis";

        public AnalyzePackageStatus(IDependencyAnalyzerRunner packageAnalyzer,
            ITargetFrameworkSelector tfmSelector,
            ILogger<AnalyzePackageStatus> logger)
        {
            Logger = logger;
            _tfmSelector = tfmSelector;
            _packageAnalyzer = packageAnalyzer ?? throw new ArgumentNullException(nameof(packageAnalyzer));
            _analysisState = null;
        }

        public async Task<IAsyncEnumerable<AnalyzeResult>> AnalyzeAsync(AnalyzeContext analysis, CancellationToken token)
        {
            if (analysis is null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var context = analysis.UpgradeContext;
            var projects = context.Projects.ToList();

            var analyzeResults = new List<AnalyzeResult>();

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

                analyzeResults.Add(new()
                {
                    FileLocation = project.FileInfo.Name,
                    Results = ExtractAnalysisResult(_analysisState)
                });
            }

            return analyzeResults.ToAsyncEnumerable();
        }

        private static IReadOnlyCollection<string> ExtractAnalysisResult(IDependencyAnalysisState? analysisState)
        {
            var results = new List<string>();

            if (analysisState is null || !analysisState.AreChangesRecommended)
            {
                results.Add("No package updates needed");
            }
            else
            {
                GetResults("References to Delete", analysisState.References.Deletions);
                GetResults("References to Add", analysisState.References.Additions);
                GetResults("Packages to Delete", analysisState.Packages.Deletions);
                GetResults("Packages to Add", analysisState.Packages.Additions);
                GetResults("Framework References to Delete", analysisState.FrameworkReferences.Deletions);
                GetResults("Framework References to Add", analysisState.FrameworkReferences.Additions);

                void GetResults<T>(string name, IReadOnlyCollection<T> collection)
                {
                    if (collection.Any())
                    {
                        results.Add(string.Concat(name, " : ", string.Join(" ; ", collection)));
                    }
                }
            }

            return results;
        }
    }
}

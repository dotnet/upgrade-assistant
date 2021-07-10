﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class AnalyzePackageStatus : IAnalyzeResultProvider
    {
        private readonly IDependencyAnalyzerRunner _packageAnalyzer;
        private readonly ITargetFrameworkSelector _tfmSelector;
        private IDependencyAnalysisState? _analysisState;
        private IAnalyzeResultProcessor _analyzeResultProcessor;

        private ILogger Logger { get; }

        public string AnalysisTypeName => "Dependency Analysis";

        public AnalyzePackageStatus(IDependencyAnalyzerRunner packageAnalyzer,
            ITargetFrameworkSelector tfmSelector,
            ILogger<AnalyzePackageStatus> logger,
            IAnalyzeResultProcessor analyzeResultProcessor)
        {
            Logger = logger;
            _tfmSelector = tfmSelector;
            _analyzeResultProcessor = analyzeResultProcessor;
            _packageAnalyzer = packageAnalyzer ?? throw new ArgumentNullException(nameof(packageAnalyzer));
            _analysisState = null;
        }

        public async Task<ICollection<AnalyzeResult>> AnalyzeAsync(AnalyzeContext analysis, CancellationToken token)
        {
            if (analysis is null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var context = analysis.UpgradeContext;
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
                analysis.Dependencies.Add(project.FileInfo.Name, _analysisState);
#pragma warning restore CS8604 // Possible null reference argument.
            }

            return _analyzeResultProcessor.Execute(analysis);
        }
    }
}

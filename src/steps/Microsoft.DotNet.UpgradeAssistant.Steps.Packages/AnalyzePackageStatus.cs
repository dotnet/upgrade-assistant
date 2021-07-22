// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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

        public string Id => "UA101";

        public string Name => "Dependency Analysis";

        public Uri InformationURI => new("https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview");

        public AnalyzePackageStatus(IDependencyAnalyzerRunner packageAnalyzer,
            ITargetFrameworkSelector tfmSelector,
            ILogger<AnalyzePackageStatus> logger)
        {
            Logger = logger;
            _tfmSelector = tfmSelector;
            _packageAnalyzer = packageAnalyzer ?? throw new ArgumentNullException(nameof(packageAnalyzer));
            _analysisState = null;
        }

        public async IAsyncEnumerable<AnalyzeResult> AnalyzeAsync(AnalyzeContext analysis, [EnumeratorCancellation] CancellationToken token)
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

                yield return new()
                {
                    FileLocation = Path.Combine(project.FileInfo.DirectoryName, project.FileInfo.Name),
                    Results = ExtractAnalysisResult(_analysisState),
                };
            }
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
                GetResults("Reference to ", " needs to be deleted ", analysisState.References.Deletions);
                GetResults("Reference to ", " needs to be added ", analysisState.References.Additions);
                GetResults("Package ", " needs to be deleted ", analysisState.Packages.Deletions);
                GetResults("Package ", " needs to be added ", analysisState.Packages.Additions);
                GetResults("Framework Reference to ", " needs to be deleted ", analysisState.FrameworkReferences.Deletions);
                GetResults("Framework Reference to ", " needs to be added ", analysisState.FrameworkReferences.Additions);

                void GetResults<T>(string name, string action, IReadOnlyCollection<T> collection)
                {
                    if (collection.Any())
                    {
                        foreach (var s in collection)
                        {
                            results.Add(string.Concat(name, s, action));
                        }
                    }
                }
            }

            return results;
        }
    }
}

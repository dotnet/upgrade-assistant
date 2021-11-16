// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageAnalyzerResultProvider : IAnalyzeResultProvider
    {
        private const string RuleId = "UA101";
        private const string RuleName = "Dependency Analysis";
        private const string FullDescription = "Dependency Analysis";
        private readonly Uri _helpUri = new("https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview");
        private readonly IDependencyAnalyzerRunner _packageAnalyzer;
        private readonly ITargetFrameworkSelector _tfmSelector;
        private IDependencyAnalysisState? _analysisState;

        private ILogger Logger { get; }

        public string Name => "Dependency Analysis";

        public Uri InformationUri => new("https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview");

        public PackageAnalyzerResultProvider(IDependencyAnalyzerRunner packageAnalyzer,
            ITargetFrameworkSelector tfmSelector,
            ILogger<PackageAnalyzerResultProvider> logger)
        {
            Logger = logger;
            _tfmSelector = tfmSelector;
            _packageAnalyzer = packageAnalyzer ?? throw new ArgumentNullException(nameof(packageAnalyzer));
            _analysisState = null;
        }

        public async Task<bool> IsApplicableAsync(AnalyzeContext analysis, CancellationToken token)
        {
            return await Task.FromResult(true);
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

                foreach (var r in this.ExtractAnalysisResult(Path.Combine(project.FileInfo.DirectoryName, project.FileInfo.Name), _analysisState))
                {
                    yield return r;
                }
            }
        }

        private HashSet<AnalyzeResult> ExtractAnalysisResult(string fileLocation, IDependencyAnalysisState? analysisState)
        {
            var results = new HashSet<AnalyzeResult>();

            if (analysisState is null || !analysisState.AreChangesRecommended)
            {
                results.Add(new()
                {
                    RuleId = RuleId,
                    RuleName = RuleName,
                    HelpUri = _helpUri,
                    FullDescription = FullDescription,
                    FileLocation = fileLocation,
                    ResultMessage = "No package updates needed.",
                });
            }
            else
            {
                GetResults("Reference to ", " needs to be deleted.", analysisState.References.Deletions);
                GetResults("Reference to ", " needs to be added.", analysisState.References.Additions);
                GetResults("Package ", " needs to be deleted.", analysisState.Packages.Deletions);
                GetResults("Package ", " needs to be added.", analysisState.Packages.Additions);
                GetResults("Framework Reference to ", " needs to be deleted.", analysisState.FrameworkReferences.Deletions);
                GetResults("Framework Reference to ", " needs to be added.", analysisState.FrameworkReferences.Additions);

                void GetResults<T>(string name, string action, IReadOnlyCollection<Operation<T>> collection)
                {
                    if (collection.Any())
                    {
                        foreach (var s in collection)
                        {
                            if (s.OperationDetails is not null && s.OperationDetails.Details is not null && s.OperationDetails.Details.Any())
                            {
                                results.UnionWith(s.OperationDetails.Details.Select(s => new AnalyzeResult()
                                {
                                    RuleId = RuleId,
                                    RuleName = RuleName,
                                    FullDescription = FullDescription,
                                    HelpUri = _helpUri,
                                    FileLocation = fileLocation,
                                    ResultMessage = s,
                                }));
                            }
                            else
                            {
                                results.Add(new()
                                {
                                    RuleId = RuleId,
                                    RuleName = RuleName,
                                    FullDescription = FullDescription,
                                    HelpUri = _helpUri,
                                    FileLocation = fileLocation,
                                    ResultMessage = string.Concat(name, s.Item, action),
                                });
                            }
                        }
                    }
                }
            }

            return results;
        }
    }
}

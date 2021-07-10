// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class DependendencyAnalyzeResultProcessor : IAnalyzeResultProcessor
    {
        public ICollection<AnalyzeResult> Execute(AnalyzeContext analyzeContext)
        {
            if (analyzeContext is null)
            {
                throw new ArgumentNullException(nameof(analyzeContext));
            }

            var analyzeResults = new List<AnalyzeResult>();

            foreach (var analysis in analyzeContext.Dependencies)
            {
                analyzeResults.Add(new()
                {
                    AnalysisName = "Dependency Analysis",
                    AnalysisFileLocation = analysis.Key,
                    AnalysisResults = ProcessAnalysisResults(analysis.Value)
                });
            }

            return analyzeResults;
        }

        public static IReadOnlyCollection<string> ProcessAnalysisResults(IDependencyAnalysisState? analysisState)
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

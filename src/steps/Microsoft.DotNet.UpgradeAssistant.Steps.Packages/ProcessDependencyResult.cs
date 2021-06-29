// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class ProcessDependencyResult : IProcessResult
    {
        public IList<Run> RunProcessResult(string toolName, Dictionary<string, IDependencyAnalysisState> analysisResults)
        {
            if (analysisResults is null)
            {
                throw new ArgumentNullException(nameof(analysisResults));
            }

            var runs = new List<Run>();
            foreach (var analysis in analysisResults)
            {
                var results = ExtractResults(analysis.Value);

                var run = new Run()
                {
                    Tool = new Tool
                    {
                        Driver = new ToolComponent
                        {
                            Name = string.Join(" ", toolName, analysis.Key)
                        }
                    },
                    Results = results
                };

                runs.Add(run);
            }

            return runs;
        }

        public static IList<Result> ExtractResults(IDependencyAnalysisState? analysisState)
        {
            IList<Result> results = new List<Result>();

            if (analysisState is null || !analysisState.AreChangesRecommended)
            {
                results.Add(new Result()
                {
                    Message = "No package updates needed".ToMessage()
                });
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
                        results.Add(GetResult(name, collection));
                    }
                }
            }

            return results;
        }

        private static Result GetResult<T>(string name, IReadOnlyCollection<T> collection)
        {
            var attachments = new List<Attachment>();
            foreach (var s in collection)
            {
                attachments.Add(new() { Description = string.Join(" ", s).ToMessage() });
            }

            return new Result()
            {
                Message = name.ToMessage(),
                Attachments = attachments
            };
        }
    }
}

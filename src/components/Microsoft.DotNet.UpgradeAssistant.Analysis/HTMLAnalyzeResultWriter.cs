// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class HTMLAnalyzeResultWriter : IAnalyzeResultWriter
    {
        private readonly ISerializer _serializer;

        public HTMLAnalyzeResultWriter(ISerializer serializer)
        {
            this._serializer = serializer;
        }

        public async Task WriteAsync(IAsyncEnumerable<AnalyzeResultDefinition> results, string? format, CancellationToken token)
        {
            if (results is null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var sarifLog = new SarifLog()
            {
                Runs = await ExtractRunsAsync(results).ToListAsync(token).ConfigureAwait(false),
            };
            WriteHTML(sarifLog);
        }

        public string GetFormat()
        {
            return "html";
        }

        private void WriteHTML(SarifLog sarifLog)
        {
            var sarifString = _serializer.Serialize(sarifLog);
            var ss = sarifString.Replace("\r\n", string.Empty).Replace(@"\", string.Empty).Replace("file:///C:/", string.Empty);

            var names = Assembly.GetExecutingAssembly();
            var templatePath = names.GetManifestResourceNames();

            using var assembly = Assembly.GetExecutingAssembly().GetManifestResourceStream(templatePath[0]);
            using var templateString = new StreamReader(assembly);
            var template = templateString.ReadToEnd();
            var finishedTemplate = template.Replace("%SARIF_LOG%", ss);
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "AnalysisReport.html"), finishedTemplate);
        }

        private static async IAsyncEnumerable<Run> ExtractRunsAsync(IAsyncEnumerable<AnalyzeResultDefinition> analyzeResultDefinitions)
        {
            await foreach (var ar in analyzeResultDefinitions)
            {
                var analyzeResults = await ar.AnalysisResults.ToListAsync().ConfigureAwait(false);
                yield return new()
                {
                    Tool = new()
                    {
                        Driver = new()
                        {
                            Name = ar.Name,
                            SemanticVersion = ar.Version,
                            InformationUri = ar.InformationUri,
                            Rules = analyzeResults.GroupBy(x => x.RuleId).Select(ExtractRule).ToList(),
                        },
                    },
                    Results = ExtractResults(analyzeResults),
                };
            }
        }

        private static ReportingDescriptor ExtractRule(IGrouping<string, AnalyzeResult> analyzeResults)
        {
            var analyzeResult = analyzeResults.First();

            var rule = new ReportingDescriptor
            {
                Id = analyzeResult.RuleId,
                HelpUri = analyzeResult.HelpUri
            };

            rule.FullDescription = new()
            {
                Text = !string.IsNullOrWhiteSpace(analyzeResult.FullDescription) ? analyzeResult.FullDescription : analyzeResult.RuleName
            };

            // TODO: once we update all the other types of rules, we will be able to remove this condition.
            if (!string.IsNullOrWhiteSpace(analyzeResult.FullDescription))
            {
                rule.Name = analyzeResult.RuleName;
            }

            return rule;
        }

        private static IList<Result> ExtractResults(IList<AnalyzeResult> analyzeResults)
        {
            var results = new List<Result>();
            foreach (var r in analyzeResults)
            {
                results.Add(new()
                {
                    RuleId = r.RuleId,
                    Message = r.ResultMessage.ToMessage(),
                    Locations = new List<Location>()
                    {
                        new()
                        {
                            PhysicalLocation = new()
                            {
                                ArtifactLocation = new()
                                {
                                    Uri = new Uri(Path.GetFullPath(r.FileLocation)),
                                },
                                Region = new()
                                {
                                    StartLine = r.LineNumber,
                                },
                            },
                        },
                    },
                });
            }

            return results;
        }
    }
}

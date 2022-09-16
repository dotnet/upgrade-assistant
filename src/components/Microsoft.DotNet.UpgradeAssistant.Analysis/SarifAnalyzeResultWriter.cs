// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Sarif;
using Newtonsoft.Json;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class SarifAnalyzeResultWriter : IOutputResultWriter
    {
        private readonly JsonSerializer _serializer = JsonSerializer.Create(new()
        {
            Formatting = Formatting.Indented,
            Culture = CultureInfo.InvariantCulture,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
        });

        public string Format => WellKnownFormats.Sarif;

        public async Task WriteAsync(IAsyncEnumerable<OutputResultDefinition> results, Stream stream, CancellationToken token)
        {
            if (results is null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var sarifLog = new SarifLog()
            {
                Runs = await ExtractRunsAsync(results).ToListAsync(token).ConfigureAwait(false),
            };

            using var writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
            using var jsonWriter = new JsonTextWriter(writer);

            _serializer.Serialize(jsonWriter, sarifLog, typeof(SarifLog));
        }

        private static async IAsyncEnumerable<Run> ExtractRunsAsync(IAsyncEnumerable<OutputResultDefinition> analyzeResultDefinitions)
        {
            await foreach (var ar in analyzeResultDefinitions)
            {
                var analyzeResults = await ar.Results.ToListAsync().ConfigureAwait(false);
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

        private static ReportingDescriptor ExtractRule(IGrouping<string, OutputResult> analyzeResults)
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
            if (!string.IsNullOrWhiteSpace(analyzeResult.RuleName))
            {
                rule.Name = analyzeResult.RuleName;
            }

            return rule;
        }

        private static IList<Result> ExtractResults(IList<OutputResult> analyzeResults)
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

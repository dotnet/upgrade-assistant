// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class AnalyzeResultWriter : IAnalyzeResultWriter
    {
        private readonly string _sarifLogPath = Path.Combine(Directory.GetCurrentDirectory(), "AnalysisReport.sarif");
        private readonly ISerializer _serializer;

        public AnalyzeResultWriter(ISerializer serializer)
        {
            this._serializer = serializer;
        }

        public async Task WriteAsync(IAsyncEnumerable<AnalyzeResultDefinition> results, CancellationToken token)
        {
            if (results is null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var sarifLog = new SarifLog()
            {
                Version = SarifVersion.Current,
                SchemaUri = new Uri("http://json.schemastore.org/sarif-2.1.0"),
                Runs = await ExtractRunsAsync(results).ConfigureAwait(false),
            };

            _serializer.Write(_sarifLogPath, sarifLog);
        }

        private static async Task<List<Run>> ExtractRunsAsync(IAsyncEnumerable<AnalyzeResultDefinition> analyzeResults)
        {
            var runs = new List<Run>();

            await foreach (var ar in analyzeResults)
            {
                var run = new Run()
                {
                    Tool = new()
                    {
                        Driver = new()
                        {
                            Name = ar.Name,
                            SemanticVersion = ar.Version,
                            InformationUri = ar.InformationURI,
                            Rules = new List<ReportingDescriptor>()
                            {
                                new()
                                {
                                    Id = ar.Id,
                                    Name = ar.Name,
                                },
                            },
                        },
                    },
                    Results = await ExtractResultsAsync(ar).ConfigureAwait(false),
                };

                runs.Add(run);
            }

            return runs;
        }

        private static async Task<IList<Result>> ExtractResultsAsync(AnalyzeResultDefinition result)
        {
            var results = new List<Result>();
            await foreach (var r in result.AnalysisResults)
            {
                GetResult(result.Id, r, results);
            }

            return results;
        }

        private static void GetResult(string id, AnalyzeResult analyzeResult, IList<Result> results)
        {
            foreach (var s in analyzeResult.Results)
            {
                results.Add(new()
                {
                    RuleId = id,
                    Message = s.ToMessage(),
                    Locations = new List<Location>()
                    {
                        new()
                        {
                            PhysicalLocation = new()
                            {
                                ArtifactLocation = new()
                                {
                                    Uri = new Uri(Path.GetFullPath(analyzeResult.FileLocation)),
                                },
                            },
                        },
                    },
                });
            }
        }
    }
}

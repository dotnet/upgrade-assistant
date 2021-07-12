// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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

        private static async Task<List<Run>> ExtractRunsAsync(IAsyncEnumerable<AnalyzeResultDef> analyzeResults)
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
                            Name = ar.AnalysisTypeName,
                        },
                    },
                    Results = await ExtractResultsAsync(ar.AnalysisResults).ConfigureAwait(false),
                };

                runs.Add(run);
            }

            return runs;
        }

        private static async Task<IList<Result>> ExtractResultsAsync(IAsyncEnumerable<AnalyzeResult> analyzeResults)
        {
            var results = new List<Result>();
            await foreach (var r in analyzeResults)
            {
                results.Add(GetResult(r.AnalysisFileLocation, r.AnalysisResults));
            }

            return results;
        }

        private static Result GetResult(string name, IReadOnlyCollection<string> collection)
        {
            var attachments = new List<Attachment>();
            foreach (var s in collection)
            {
                attachments.Add(new()
                {
                    Description = s.ToMessage(),
                    ArtifactLocation = new()
                    {
                        Uri = new Uri(Path.GetFullPath(name)),
                    },
                });
            }

            return new Result()
            {
                Message = name.ToMessage(),
                Attachments = attachments,
            };
        }

        public async Task WriteAsync(IAsyncEnumerable<AnalyzeResultDef> results, CancellationToken token)
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
    }
}

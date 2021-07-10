// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class AnalyzeResultWriter : IAnalyzeResultWriter
    {
        private ISerializer _serializer;
        private string _sarifLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "AnalysisReport.sarif");

        public AnalyzeResultWriter(ISerializer serializer)
        {
            _serializer = serializer;
        }

        private static List<Run> ExtractRuns(Dictionary<string, ICollection<AnalyzeResult>> analyzeResults)
        {
            var runs = new List<Run>();

            foreach (var kvp in analyzeResults)
            {
                var run = new Run()
                {
                    Tool = new()
                    {
                        Driver = new()
                        {
                            Name = kvp.Key
                        }
                    },
                    Results = ExtractResults(kvp.Value)
                };

                runs.Add(run);
            }

            return runs;
        }

        private static IList<Result> ExtractResults(ICollection<AnalyzeResult> analyzeResults)
        {
            var results = new List<Result>();
            foreach (var r in analyzeResults)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                results.Add(GetResult(r.AnalysisFileLocation, r.AnalysisResults));
#pragma warning restore CS8604 // Possible null reference argument.
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
                        Uri = new Uri(Path.GetFullPath(name))
                    }
                });
            }

            return new Result()
            {
                Message = name.ToMessage(),
                Attachments = attachments
            };
        }

        public void WriteAsync(Dictionary<string, ICollection<AnalyzeResult>> results, CancellationToken token)
        {
            if (results is null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var sarifLog = new SarifLog()
            {
                Version = SarifVersion.Current,
                SchemaUri = new Uri("http://json.schemastore.org/sarif-2.1.0"),
                Runs = ExtractRuns(results)
            };

            _serializer.Write(_sarifLogFilePath, sarifLog);
        }
    }
}

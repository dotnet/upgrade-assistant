// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Sarif;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis.Tests
{
    public class AnalyzeResultWriterTests
    {
        [Fact]
        public async Task ValidateSarifVersion()
        {
            var serializer = new JsonSerializer();
            using var source = new CancellationTokenSource();
            var writer = new AnalyzeResultWriter(serializer);

            var analyzeResults = new List<AnalyzeResult>
            {
                new AnalyzeResult
                {
                    FileLocation = "some-file-path",
                    LineNumber = 1,
                    ResultMessage = "some result message",
                    RuleId = "RULE0001",
                    RuleName = "RuleName0001"
                }
            };

            var analyzeResultMap = new List<AnalyzeResultDefinition>
            {
                new AnalyzeResultDefinition
                {
                    Name = "some-name",
                    Version = "1.0.0",
                    InformationURI = new Uri("https://github.com/dotnet/upgrade-assistant"),
                    AnalysisResults = analyzeResults.ToAsyncEnumerable()
                }
            };

            await writer.WriteAsync(analyzeResultMap.ToAsyncEnumerable(), source.Token).ConfigureAwait(false);

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "AnalysisReport.sarif");
            if (!File.Exists(filePath))
            {
                Assert.True(false, "File wasn't exported successfully.");
            }

            var sarifLog = SarifLog.Load(filePath);
            Assert.Equal("https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.5.json", sarifLog.SchemaUri.OriginalString);
            Assert.Equal(SarifVersion.Current, sarifLog.Version);
        }
    }
}

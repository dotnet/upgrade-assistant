﻿// Licensed to the .NET Foundation under one or more agreements.
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
        public async Task ShouldThrowIfResultsIsNull()
        {
            var serializer = new JsonSerializer();
            using var source = new CancellationTokenSource();
            var writer = new AnalyzeResultWriter(serializer);

            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await writer.WriteAsync(null, source.Token).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task ValidateSarifMetadata()
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
                    RuleName = "RuleName0001",
                    FullDescription = "some full description",
                    HelpUri = new Uri("https://github.com/dotnet/upgrade-assistant")
                }
            };

            var analyzeResultMap = new List<AnalyzeResultDefinition>
            {
                new AnalyzeResultDefinition
                {
                    Name = "some-name",
                    Version = "1.0.0",
                    InformationUri = new Uri("https://github.com/dotnet/upgrade-assistant"),
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

            var analyzeResult = analyzeResults.First();
            var rule = sarifLog.Runs[0].Tool.Driver.Rules.First();
            Assert.Equal(analyzeResult.RuleId, rule.Id);
            Assert.Equal(analyzeResult.RuleName, rule.Name);
            Assert.Equal(analyzeResult.FullDescription, rule.FullDescription.Text);
            Assert.Equal(analyzeResult.HelpUri, rule.HelpUri);
        }

        [Fact]
        public async Task ValidateSarifRuleWhenFullDescriptionIsEmpty()
        {
            var serializer = new JsonSerializer();
            using var source = new CancellationTokenSource();
            var writer = new AnalyzeResultWriter(serializer);

            // This result is similar to the result generated by roslyn analyzers.
            var analyzeResults = new List<AnalyzeResult>
            {
                new AnalyzeResult
                {
                    FileLocation = "some-file-path",
                    LineNumber = 1,
                    ResultMessage = "some result message",
                    RuleId = "RULE0001",
                    RuleName = "RuleName0001",
                    HelpUri = new Uri("https://github.com/dotnet/upgrade-assistant")
                }
            };

            var analyzeResultMap = new List<AnalyzeResultDefinition>
            {
                new AnalyzeResultDefinition
                {
                    Name = "some-name",
                    Version = "1.0.0",
                    InformationUri = new Uri("https://github.com/dotnet/upgrade-assistant"),
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

            var analyzeResult = analyzeResults.First();
            var rule = sarifLog.Runs[0].Tool.Driver.Rules.First();
            Assert.Equal(analyzeResult.RuleId, rule.Id);
            Assert.Equal(analyzeResult.RuleName, rule.FullDescription.Text);
            Assert.Equal(analyzeResult.HelpUri, rule.HelpUri);
        }
    }
}

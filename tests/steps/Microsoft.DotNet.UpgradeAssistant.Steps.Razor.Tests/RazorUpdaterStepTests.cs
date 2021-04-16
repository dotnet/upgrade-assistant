// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public class RazorUpdaterStepTests
    {
        private static readonly Regex GeneratedSourceFilePathRegex = new Regex("^#pragma checksum \"(?<Path>.*?)\"", RegexOptions.Compiled);

        [Fact]
        public void CtorTests()
        {
            // Arrange
            using var mock = GetMock(false, "TestViews/Test.csproj", 1, 2);

            // Act
            var step = mock.Create<RazorUpdaterStep>();

            // Assert
            Assert.Collection(
                step.DependencyOf,
                d => Assert.Equal(WellKnownStepIds.NextProjectStepId, d));
            Assert.Collection(
                step.DependsOn.OrderBy(x => x),
                d => Assert.Equal(WellKnownStepIds.BackupStepId, d),
                d => Assert.Equal(WellKnownStepIds.SetTFMStepId, d),
                d => Assert.Equal(WellKnownStepIds.TemplateInserterStepId, d));
            Assert.Equal("Update Razor files using registered Razor updaters", step.Description);
            Assert.Equal(WellKnownStepIds.RazorUpdaterStepId, step.Id);
            Assert.Equal("Update Razor files", step.Title);
            Assert.Collection(
                step.SubSteps.Select(s => s.Id),
                s => Assert.Equal("RazorUpdater #0", s),
                s => Assert.Equal("RazorUpdater #1", s),
                s => Assert.Equal("RazorUpdater #2", s));
            Assert.Equal(UpgradeStepStatus.Unknown, step.Status);
            Assert.False(step.IsDone);
            Assert.Empty(step.RazorDocuments);
        }

        [Fact]
        public void NegativeCtorTests()
        {
            Assert.Throws<ArgumentNullException>("razorUpdaters", () => new RazorUpdaterStep(null!, new NullLogger<RazorUpdaterStep>()));
            Assert.Throws<ArgumentNullException>("logger", () => new RazorUpdaterStep(Enumerable.Empty<IUpdater<RazorCodeDocument>>(), null!));
        }

        [Theory]
        [InlineData("TestViews/Test.csproj", 0, 2, true, true)] // Vanilla positive case
        [InlineData("TestViews/Test.csproj", 1, 1, false, false)] // Not applicable if no project is loaded
        [InlineData("TestViews/Test.csproj", 0, 0, true, false)] // Not applicable if there are no updaters
        [InlineData("NoViews/Test.csproj", 0, 1, true, false)] // Not applicable if there are no Razor pages
        [InlineData("Test.csproj", 1, 0, true, true)] // Applicable even with only complete updaters (updater status is not checked)
        public async Task IsApplicableTests(string projectPath, int completeUpdaterCount, int incompleteUpdaterCount, bool projectLoaded, bool expected)
        {
            // Arrange
            using var mock = GetMock(projectLoaded, projectPath, completeUpdaterCount, incompleteUpdaterCount);
            var step = mock.Create<RazorUpdaterStep>();
            var context = mock.Mock<IUpgradeContext>();

            // Act
            var result = await step.IsApplicableAsync(context.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task IsApplicableNegativeTests()
        {
            using var mock = GetMock(true, "Test.csproj", 1, 1);
            var step = mock.Create<RazorUpdaterStep>();
            await Assert.ThrowsAsync<ArgumentNullException>("context", () => step.IsApplicableAsync(null!, CancellationToken.None)).ConfigureAwait(true);
        }

        [Theory]
        [InlineData("Test.csproj", 0, 2, true, new[] { "_ViewImports.cshtml", "Invalid.cshtml", "Simple.cshtml", "View.cshtml" })] // Vanilla positive case
        [InlineData("TestViews/Test.csproj", 2, 2, false, new[] { "Simple.cshtml", "View.cshtml" })] // Mixed complete and incomplete updaters and no _ViewImports
        [InlineData("Test.csproj", 2, 0, true, new[] { "_ViewImports.cshtml", "Invalid.cshtml", "Simple.cshtml", "View.cshtml" })] // Test with no incomplete updaters
        [InlineData("NoViews/Test.csproj", 2, 2, true, new string[0])] // Test with no Razor documents
        [InlineData("Test.csproj", 0, 0, true, new[] { "_ViewImports.cshtml", "Invalid.cshtml", "Simple.cshtml", "View.cshtml" })] // No sub-steps
        public async Task InitializeTests(string projectPath, int completeUpdaterCount, int incompleteUpdaterCount, bool expectImports, string[] expectedFiles)
        {
            // Arrange
            using var mock = GetMock(true, projectPath, completeUpdaterCount, incompleteUpdaterCount);
            var step = mock.Create<RazorUpdaterStep>();
            var context = mock.Mock<IUpgradeContext>();
            Assert.Equal(UpgradeStepStatus.Unknown, step.Status);
            Assert.Equal(string.Empty, step.StatusDetails);
            Assert.Equal(BuildBreakRisk.Unknown, step.Risk);

            // Act
            await step.InitializeAsync(context.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            // Confirm status and associated properties are updated, as expected
            if (incompleteUpdaterCount > 0)
            {
                Assert.Equal(UpgradeStepStatus.Incomplete, step.Status);
                Assert.Equal($"{incompleteUpdaterCount} Razor updaters need applied", step.StatusDetails);
                Assert.Equal(BuildBreakRisk.Medium, step.Risk);
                Assert.False(step.IsDone);
            }
            else
            {
                Assert.Equal(UpgradeStepStatus.Complete, step.Status);
                Assert.Equal("No Razor updaters need applied", step.StatusDetails);
                Assert.Equal(BuildBreakRisk.None, step.Risk);
                Assert.True(step.IsDone);
            }

            // Confirm that RazorDocuments are correctly populated
            Assert.Collection(step.RazorDocuments, expectedFiles.Select(f => ValidateRazorDocument(expectImports, f)).ToArray());

            var subSteps = step.SubSteps.ToArray();
            for (var i = 0; i < completeUpdaterCount + incompleteUpdaterCount; i++)
            {
                // Confirm that sub-steps are initialized as expected
                Assert.Equal(i >= completeUpdaterCount ? UpgradeStepStatus.Incomplete : UpgradeStepStatus.Complete, subSteps[i].Status);
            }
        }

        [Fact]
        public async Task InitializeNegativeTests()
        {
            using var mock = GetMock(false, "Test.csproj", 0, 1);
            var step = mock.Create<RazorUpdaterStep>();
            var context = mock.Mock<IUpgradeContext>();

            // Null context
            await Assert.ThrowsAsync<ArgumentNullException>("context", () => step.InitializeAsync(null!, CancellationToken.None)).ConfigureAwait(true);

            // No project
            await step.InitializeAsync(context.Object, CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(UpgradeStepStatus.Failed, step.Status);
        }

        [Theory]
        [InlineData("Test.csproj", true, new[] { "_ViewImports.cshtml", "Invalid.cshtml", "Simple.cshtml", "View.cshtml" })] // Vanilla positive case
        [InlineData("TestViews/Test.csproj", false, new[] { "Simple.cshtml", "View.cshtml" })] // No _ViewImports
        [InlineData("NoViews/Test.csproj", false, new string[0])] // No Razor documents

        public async Task ProcessRazorDocumentsTests(string projectPath, bool expectImports, string[] expectedFiles)
        {
            // Arrange
            using var mock = GetMock(true, projectPath, 0, 1);
            var step = mock.Create<RazorUpdaterStep>();
            var context = mock.Mock<IUpgradeContext>();
            Assert.Throws<InvalidOperationException>(() => step.ProcessRazorDocuments(null));
            await step.InitializeAsync(context.Object, CancellationToken.None).ConfigureAwait(true);
            step.ClearRazorDocuments();

            // Act
            step.ProcessRazorDocuments(null);

            // Assert
            // Confirm that RazorDocuments are correctly populated
            Assert.Collection(step.RazorDocuments, expectedFiles.Select(f => ValidateRazorDocument(expectImports, f)).ToArray());
        }

        [Fact]
        public async Task ResetTests()
        {
            // Arrange
            using var mock = GetMock(true, "Test.csproj", 1, 1);
            var step = mock.Create<RazorUpdaterStep>();
            var context = mock.Mock<IUpgradeContext>();
            await step.InitializeAsync(context.Object, CancellationToken.None).ConfigureAwait(true);

            // Act
            step.Reset();

            // Assert
            Assert.Throws<InvalidOperationException>(() => step.ProcessRazorDocuments(null));
            Assert.Equal(UpgradeStepStatus.Unknown, step.Status);
            Assert.Equal(string.Empty, step.StatusDetails);
            Assert.Equal(BuildBreakRisk.Unknown, step.Risk);

            foreach (var subStep in step.SubSteps)
            {
                Assert.Equal(UpgradeStepStatus.Unknown, subStep.Status);
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        public async Task ApplyTests(int completeUpdaterCount, int incompleteUpdaterCount)
        {
            // Arrange
            using var mock = GetMock(true, "Test.csproj", completeUpdaterCount, incompleteUpdaterCount);
            var step = mock.Create<RazorUpdaterStep>();
            var context = mock.Mock<IUpgradeContext>();
            await step.InitializeAsync(context.Object, CancellationToken.None).ConfigureAwait(true);
            step.SetStatus(UpgradeStepStatus.Incomplete);

            // Act
            await step.ApplyAsync(context.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            if (incompleteUpdaterCount > 0)
            {
                Assert.Equal(UpgradeStepStatus.Incomplete, step.Status);
                Assert.Equal($"{incompleteUpdaterCount} Razor updaters need applied", step.StatusDetails);
            }
            else
            {
                Assert.Equal(UpgradeStepStatus.Complete, step.Status);
                Assert.Equal(string.Empty, step.StatusDetails);
            }
        }

        [Fact]
        public async Task NegativeApplyTests()
        {
            using var mock = GetMock(true, "Test.csproj", 1, 1);
            var step = mock.Create<RazorUpdaterStep>();
            await Assert.ThrowsAsync<ArgumentNullException>("context", () => step.ApplyAsync(null!, CancellationToken.None)).ConfigureAwait(true);
        }

        private static Action<RazorCodeDocument> ValidateRazorDocument(bool expectImports, string fileName)
        {
            return doc =>
            {
                // Get the Razor documents code and syntax
                var generatedCode = doc.GetCSharpDocument();
                var syntax = doc.GetSyntaxTree();

                // Confirm that _ViewImports contents are either included or not depending on whether or not they're
                // expected in this scenario.
                if (expectImports)
                {
                    Assert.Contains("_ViewImports.cshtml", generatedCode.GeneratedCode, StringComparison.Ordinal);
                }
                else
                {
                    Assert.DoesNotContain("_ViewImports.cshtml", generatedCode.GeneratedCode, StringComparison.Ordinal);
                }

                // Confirm that the generated C# came from a file with the right name
                var sourceFileMatch = GeneratedSourceFilePathRegex.Match(generatedCode.GeneratedCode);
                Assert.True(sourceFileMatch.Success);
                var generatedCoudeSourcePath = sourceFileMatch.Groups["Path"].Value;
                Assert.Equal(fileName, Path.GetFileName(generatedCoudeSourcePath));

                // Confirm that the syntax tree comes from a file with the right name
                Assert.Equal(fileName, Path.GetFileName(syntax.Source.FilePath));
            };
        }

        private static AutoMock GetMock(bool projectLoaded, string projectPath, int completeUpdaterCount, int incompleteUpdaterCount)
        {
            var mock = AutoMock.GetLoose(cfg =>
            {
                if (completeUpdaterCount + incompleteUpdaterCount == 0)
                {
                    cfg.RegisterInstance(Enumerable.Empty<IUpdater<RazorCodeDocument>>());
                }
                else
                {
                    for (var i = 0; i < completeUpdaterCount + incompleteUpdaterCount; i++)
                    {
                        var mock = new Mock<IUpdater<RazorCodeDocument>>();
                        mock.Setup(c => c.Id).Returns($"RazorUpdater #{i}");
                        mock.Setup(c => c.IsApplicableAsync(It.IsAny<IUpgradeContext>(),
                                                            It.IsAny<ImmutableArray<RazorCodeDocument>>(),
                                                            It.IsAny<CancellationToken>())).Returns(Task.FromResult<IUpdaterResult>(new FileUpdaterResult(i >= completeUpdaterCount, Enumerable.Empty<string>())));
                        mock.Setup(c => c.Risk).Returns(BuildBreakRisk.Medium);
                        cfg.RegisterMock(mock);
                    }
                }
            });

            var project = projectLoaded ? mock.Mock<IProject>() : null;
            project?.Setup(p => p.FileInfo).Returns(new FileInfo(projectPath));

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.CurrentProject).Returns(project?.Object);

            return mock;
        }
    }
}

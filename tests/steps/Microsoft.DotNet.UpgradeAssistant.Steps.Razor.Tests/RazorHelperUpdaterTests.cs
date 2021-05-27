﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public class RazorHelperUpdaterTests
    {
        [Fact]
        public void CtorNegativeTests()
        {
            var mock = AutoMock.GetLoose();

            Assert.Throws<ArgumentNullException>("helperMatcher", () => new RazorHelperUpdater(null!, mock.Mock<ILogger<RazorHelperUpdater>>().Object));
            Assert.Throws<ArgumentNullException>("logger", () => new RazorHelperUpdater(mock.Mock<IHelperMatcher>().Object, null!));
        }

        [Fact]
        public void PropertyTests()
        {
            var mock = AutoMock.GetLoose();
            var updater = mock.Create<RazorHelperUpdater>();

            Assert.Equal("Microsoft.DotNet.UpgradeAssistant.Steps.Razor.RazorHelperUpdater", updater.Id);
            Assert.Equal("Replace @helper syntax in Razor files", updater.Title);
            Assert.Equal("Update Razor documents to use local methods instead of @helper functions", updater.Description);
            Assert.Equal(BuildBreakRisk.Low, updater.Risk);
        }

        [Fact]
        public async Task IsApplicableNegativeTests()
        {
            using var mock = await GetMock(null).ConfigureAwait(true);
            var updater = mock.Create<RazorHelperUpdater>();

            await Assert.ThrowsAsync<ArgumentNullException>("context", () => updater.IsApplicableAsync(null!, Enumerable.Empty<RazorCodeDocument>().ToImmutableArray(), CancellationToken.None))
                .ConfigureAwait(true);
        }

        [Fact]
        public async Task ApplyNegativeTests()
        {
            using var mock = await GetMock(null).ConfigureAwait(true);
            var updater = mock.Create<RazorHelperUpdater>();

            await Assert.ThrowsAsync<ArgumentNullException>("context", () => updater.ApplyAsync(null!, Enumerable.Empty<RazorCodeDocument>().ToImmutableArray(), CancellationToken.None))
                .ConfigureAwait(true);
        }

        [Theory]
        [MemberData(nameof(IsApplicableData))]
        public async Task IsApplicableTests(string projectPath, FileUpdaterResult expectedResult)
        {
            // Arrange
            using var mock = await GetMock(projectPath).ConfigureAwait(true);
            var context = mock.Mock<IUpgradeContext>();
            var updater = mock.Create<RazorHelperUpdater>();
            var inputs = await GetRazorCodeDocumentsAsync(mock).ConfigureAwait(true);

            // Act
            var result = await updater.IsApplicableAsync(context.Object, inputs, CancellationToken.None).ConfigureAwait(true);

            // Assert
            var fileUpdaterResult = Assert.IsType<FileUpdaterResult>(result);
            Assert.Equal(expectedResult.Result, fileUpdaterResult.Result);
            Assert.Collection(fileUpdaterResult.FilePaths, expectedResult.FilePaths.Select<string, Action<string>>(e => a => Assert.EndsWith(e, a, StringComparison.Ordinal)).ToArray());
        }

        public static IEnumerable<object[]> IsApplicableData =>
            new List<object[]>
            {
                new object[]
                {
                    "RazorHelperUpdaterViews/OneHelper/Test.csproj",
                    new FileUpdaterResult(true, new[] { @"\MyView.cshtml" })
                },
                new object[]
                {
                    "RazorHelperUpdaterViews/HelperInSubDir/Test.csproj",
                    new FileUpdaterResult(true, new[] { @"\OneHelper\MyView.cshtml", @"\OneHelper\AnotherHelper\MyView.cshtml" })
                },
                new object[]
                {
                    "RazorHelperUpdaterViews/MultipleHelpers/Test.csproj",
                    new FileUpdaterResult(true, new[] { @"\MultiHelpers.cshtml", @"\MyView.cshtml" })
                },
                new object[]
                {
                    "RazorHelperUpdaterViews/NoHelpers/Test.csproj",
                    new FileUpdaterResult(false, Array.Empty<string>())
                }
            };

        private static async Task<AutoMock> GetMock(string? projectPath)
        {
            if (projectPath is not null)
            {
                // Create a temporary working directory and copy views to it so that they can be updated
                var workingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                var dir = Directory.CreateDirectory(workingDir);
                Assert.True(dir.Exists);
                await FileHelpers.CopyDirectoryAsync(Path.GetDirectoryName(projectPath)!, workingDir).ConfigureAwait(false);
                projectPath = Path.Combine(workingDir, Path.GetFileName(projectPath));
            }

            var mock = AutoMock.GetLoose(cfg =>
            {
                cfg.RegisterType<HelperMatcher>().As<IHelperMatcher>();
            });

            var projectFile = mock.Mock<IProjectFile>();
            var project = projectPath is not null ? mock.Mock<IProject>() : null;
            project?.Setup(p => p.FileInfo).Returns(new FileInfo(projectPath!));
            project?.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project?.Setup(p => p.GetRoslynProject()).Returns(() =>
            {
                var ws = new AdhocWorkspace();
                var name = Path.GetFileNameWithoutExtension(projectPath)!;
                return ws.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, name, name, "C#", filePath: projectPath));
            });
            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.CurrentProject).Returns(project?.Object);

            return mock;
        }

        private static async Task<ImmutableArray<RazorCodeDocument>> GetRazorCodeDocumentsAsync(AutoMock mock)
        {
            var updaterStep = new RazorUpdaterStep(Enumerable.Empty<IUpdater<RazorCodeDocument>>(), mock.Mock<ILogger<RazorUpdaterStep>>().Object);
            await updaterStep.InitializeAsync(mock.Mock<IUpgradeContext>().Object, CancellationToken.None).ConfigureAwait(true);
            return updaterStep.RazorDocuments;
        }
    }
}

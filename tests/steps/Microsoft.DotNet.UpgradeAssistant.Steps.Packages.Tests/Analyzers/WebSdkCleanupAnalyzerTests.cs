// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers.Tests
{
    public class WebSdkCleanupAnalyzerTests
    {
        [Fact]
        public void CtorTests()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            // Act
            var analyzer = mock.Create<WebSdkCleanupAnalyzer>();

            // Assert
            Assert.Equal("Web SDK cleanup analyzer", analyzer.Name);
        }

        [Fact]
        public void NegativeCtorTests()
        {
            Assert.Throws<ArgumentNullException>("logger", () => new WebSdkCleanupAnalyzer(null!));
        }

        [Theory]
        [InlineData("Microsoft.NET.Sdk.Web", new[] { "Microsoft.AspNetCore.App" }, new[] { "Microsoft.AspNetCore.App" })] // Vanilla positive case
        [InlineData("Microsoft.NET.Sdk.Web", new[] { "X", "Y", "Microsoft.AspNetCore.App", "Z" }, new[] { "Microsoft.AspNetCore.App" })] // Multiple references
        [InlineData("Microsoft.NET.Sdk.Web", new[] { "Microsoft.AspNetCore.App", "Microsoft.AspNetCore.App" }, new[] { "Microsoft.AspNetCore.App" })] // Duplicate references
        [InlineData("microsoft.NET.SDK.Web", new[] { "microsoft.ASPNetCore.App" }, new[] { "microsoft.ASPNetCore.App" })] // Ununusual case
        [InlineData("Microsoft.NET.Sdk", new[] { "Microsoft.AspNetCore.App" }, new string[0])] // Wrong SDK
        [InlineData("Microsoft.NET.Sdk.Web", new[] { "A", "B", "C" }, new string[0])] // Wrong framework reference
        [InlineData(null, new[] { "Microsoft.AspNetCore.App" }, new string[0])] // No SDK
        [InlineData("Microsoft.NET.Sdk.Web", new string[0], new string[0])] // No framework reference
        [InlineData("Microsoft.NET.Sdk.Web", null, new string[0])] // Null framework references
        public async Task AnalyzeAsyncTests(string? sdk, string[]? frameworkReferences, string[] expectedReferencesToRemove)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            (var project, var state) = await GetMockProjectAndPackageState(mock, sdk, frameworkReferences?.Select(r => new Reference(r))).ConfigureAwait(true);
            var analyzer = mock.Create<WebSdkCleanupAnalyzer>();

            // Act
            var finalState = await analyzer.AnalyzeAsync(project, state, CancellationToken.None).ConfigureAwait(true);

            // Assert
            Assert.Equal(state, finalState);
            Assert.Collection(
                state.FrameworkReferencesToRemove,
                expectedReferencesToRemove.Select<string, Action<Reference>>(e => r => Assert.Equal(e, r.Name)).ToArray());
        }

        [Fact]
        public async Task NegativeAnalyzeAsyncTests()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            (var project, var state) = await GetMockProjectAndPackageState(mock).ConfigureAwait(true);
            var analyzer = mock.Create<WebSdkCleanupAnalyzer>();

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>("state", () => analyzer.AnalyzeAsync(project, null!, CancellationToken.None)).ConfigureAwait(true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => analyzer.AnalyzeAsync(null!, state, CancellationToken.None)).ConfigureAwait(true);
        }

        private static async Task<(IProject Project, PackageAnalysisState State)> GetMockProjectAndPackageState(AutoMock mock, string? sdk = null, IEnumerable<Reference>? frameworkReferences = null)
        {
            var projectRoot = mock.Mock<IProjectFile>();
            projectRoot.Setup(r => r.IsSdk).Returns(sdk is not null);
            if (sdk is not null)
            {
                projectRoot.Setup(r => r.Sdk).Returns(sdk);
            }
            else
            {
                projectRoot.Setup(r => r.Sdk).Throws<ArgumentOutOfRangeException>();
            }

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectRoot.Object);
            project.Setup(p => p.FrameworkReferences).Returns(frameworkReferences!);

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.CurrentProject).Returns(project.Object);

            var restorer = mock.Mock<IPackageRestorer>();
            restorer.Setup(r => r.RestorePackagesAsync(
                It.IsAny<IUpgradeContext>(),
                It.IsAny<IProject>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(new RestoreOutput(string.Empty)));

            return (project.Object, await PackageAnalysisState.CreateAsync(context.Object, restorer.Object, CancellationToken.None).ConfigureAwait(true));
        }
    }
}

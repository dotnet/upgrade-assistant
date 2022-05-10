// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Web.Tests
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
        [InlineData(new[] { "Microsoft.NET.Sdk.Web" }, new[] { "Microsoft.AspNetCore.App" }, new[] { "Microsoft.AspNetCore.App" })] // Vanilla positive case
        [InlineData(new[] { "Microsoft.NET.Sdk.Web" }, new[] { "X", "Y", "Microsoft.AspNetCore.App", "Z" }, new[] { "Microsoft.AspNetCore.App" })] // Multiple references
        [InlineData(new[] { "Microsoft.NET.Sdk.Web" }, new[] { "Microsoft.AspNetCore.App", "Microsoft.AspNetCore.App" }, new[] { "Microsoft.AspNetCore.App" })] // Duplicate references
        [InlineData(new[] { "microsoft.NET.SDK.Web" }, new[] { "microsoft.ASPNetCore.App" }, new[] { "microsoft.ASPNetCore.App" })] // Ununusual case
        [InlineData(new[] { "Microsoft.NET.Sdk" }, new[] { "Microsoft.AspNetCore.App" }, new string[0])] // Wrong SDK
        [InlineData(new[] { "Microsoft.NET.Sdk.Web" }, new[] { "A", "B", "C" }, new string[0])] // Wrong framework reference
        [InlineData(null, new[] { "Microsoft.AspNetCore.App" }, new string[0])] // No SDK
        [InlineData(new[] { "Microsoft.NET.Sdk.Web" }, new string[0], new string[0])] // No framework reference
        [InlineData(new[] { "Microsoft.NET.Sdk.Web" }, null, new string[0])] // Null framework references
        public async Task AnalyzeAsyncTests(string[]? sdk, string[]? frameworkReferences, string[] expectedReferencesToRemove)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var dependency = new Mock<IDependencyCollection<Reference>>();

            var state = new Mock<IDependencyAnalysisState>();
            state.Setup(s => s.FrameworkReferences).Returns(dependency.Object);

            var project = GetMockProjectAndPackageState(mock, sdk, frameworkReferences?.Select(r => new Reference(r)));
            var analyzer = mock.Create<WebSdkCleanupAnalyzer>();

            // Act
            await analyzer.AnalyzeAsync(project, state.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            foreach (var expected in expectedReferencesToRemove)
            {
                dependency.Verify(d => d.Remove(new Reference(expected), It.IsAny<OperationDetails>()));
            }
        }

        [Fact]
        public async Task NegativeAnalyzeAsyncTests()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var dependency = new Mock<IDependencyCollection<Reference>>();

            var state = new Mock<IDependencyAnalysisState>();
            state.Setup(s => s.FrameworkReferences).Returns(dependency.Object);

            var project = GetMockProjectAndPackageState(mock);
            var analyzer = mock.Create<WebSdkCleanupAnalyzer>();

            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>("state", () => analyzer.AnalyzeAsync(project, null!, CancellationToken.None)).ConfigureAwait(true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => analyzer.AnalyzeAsync(null!, state.Object, CancellationToken.None)).ConfigureAwait(true);
        }

        private static IProject GetMockProjectAndPackageState(AutoMock mock, string[]? sdk = null, IEnumerable<Reference>? frameworkReferences = null)
        {
            var projectRoot = mock.Mock<IProjectFile>();
            projectRoot.Setup(r => r.IsSdk).Returns(sdk is not null);
            if (sdk is not null)
            {
                projectRoot.Setup(r => r.Sdk).Returns(new HashSet<string>(sdk, StringComparer.OrdinalIgnoreCase));
            }
            else
            {
                projectRoot.Setup(r => r.Sdk).Returns(Array.Empty<string>());
            }

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectRoot.Object);
            project.Setup(p => p.FrameworkReferences).Returns(frameworkReferences!);

            return project.Object;
        }
    }
}

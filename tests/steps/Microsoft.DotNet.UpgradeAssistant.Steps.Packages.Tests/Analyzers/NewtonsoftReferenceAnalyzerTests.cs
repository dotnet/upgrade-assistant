// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Tests.Analyzers
{
    /// <summary>
    /// Unit tests for the NewtonsoftReferenceAnalyzer.
    /// </summary>
    [Collection(MSBuildStepTestCollection.Name)]
    public class NewtonsoftReferenceAnalyzerTests
    {
        /// <summary>
        /// Validates that the analyzer will only be applied when TFM is not net48.
        /// </summary>
        /// <returns>a task.</returns>
        [Fact]
        public async Task AnalyzerIsApplicableToAspNetCoreWebProject()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var analyzer = mock.Create<NewtonsoftReferenceAnalyzer>();
            using var tokenSource = new CancellationTokenSource();
            var project = mock.Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(new[] { new TargetFrameworkMoniker("net5.0") });
            project.Setup(p => p.Components).Returns(ProjectComponents.AspNetCore);
            project.Setup(p => p.OutputType).Returns(ProjectOutputType.Exe);

            // Act
            var actual = await analyzer.IsApplicableAsync(project.Object, tokenSource.Token).ConfigureAwait(false);

            // Assert
            Assert.True(actual, "Expected true because the mock is an ASP.NET Core project");
        }

        /// <summary>
        /// Validates that the analyzer will not be applied to net48 TFM.
        /// </summary>
        /// <returns>a task.</returns>
        [Fact]
        public async Task AnalyzerIsNotApplicableToNetFrameworkProjects()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var analyzer = mock.Create<NewtonsoftReferenceAnalyzer>();
            using var tokenSource = new CancellationTokenSource();
            var project = mock.Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(new[] { new TargetFrameworkMoniker("net472") });
            project.Setup(p => p.Components).Returns(ProjectComponents.AspNetCore);
            project.Setup(p => p.OutputType).Returns(ProjectOutputType.Exe);

            // Act
            var actual = await analyzer.IsApplicableAsync(project.Object, tokenSource.Token).ConfigureAwait(false);

            // Assert
            Assert.False(actual, "Expected false because the mock is an ASP.NET Framework project");
        }

        /// <summary>
        /// Validates that the analyzer will not be applied to library projects.
        /// </summary>
        /// <returns>a task.</returns>
        [Fact]
        public async Task AnalyzerIsNotApplicableToLibraryProjects()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var analyzer = mock.Create<NewtonsoftReferenceAnalyzer>();
            using var tokenSource = new CancellationTokenSource();
            var project = mock.Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(new[] { new TargetFrameworkMoniker("net5.0") });
            project.Setup(p => p.Components).Returns(ProjectComponents.AspNetCore);
            project.Setup(p => p.OutputType).Returns(ProjectOutputType.Library);

            // Act
            var actual = await analyzer.IsApplicableAsync(project.Object, tokenSource.Token).ConfigureAwait(false);

            // Assert
            Assert.False(actual, "Expected false because the mock is not exe project");
        }

        /// <summary>
        /// Validates that the analyzer will not be applied to net48 TFM.
        /// </summary>
        /// <returns>a task.</returns>
        [Fact]
        public async Task AnalyzerIsOnlyApplicableToAspNetProjects()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var analyzer = mock.Create<NewtonsoftReferenceAnalyzer>();
            using var tokenSource = new CancellationTokenSource();
            var project = mock.Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(new[] { new TargetFrameworkMoniker("net5.0") });
            project.Setup(p => p.Components).Returns(ProjectComponents.Wpf);
            project.Setup(p => p.OutputType).Returns(ProjectOutputType.Exe);

            // Act
            var actual = await analyzer.IsApplicableAsync(project.Object, tokenSource.Token).ConfigureAwait(false);

            // Assert
            Assert.False(actual, "Expected false because the mock is not an ASP.NET project");
        }
    }
}

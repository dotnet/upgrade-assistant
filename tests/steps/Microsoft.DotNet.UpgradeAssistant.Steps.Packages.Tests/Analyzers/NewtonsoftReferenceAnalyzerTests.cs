// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Tests.Analyzers
{
    /// <summary>
    /// Unit tests for the NewtonsoftReferenceAnalyzer.
    /// </summary>
    public class NewtonsoftReferenceAnalyzerTests
    {
        private const string NewtonsoftPackageName = "Microsoft.AspNetCore.Mvc.NewtonsoftJson";

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
            var project = CreateProjectForWhichAnalyzerIsApplicable(mock);
            var packageState = await CreatePackageAnalysisState(mock, project).ConfigureAwait(false);
            var packageLoader = CreatePackageLoader(mock);

            // Act
            var actual = await analyzer.AnalyzeAsync(project.Object, packageState, default).ConfigureAwait(false);

            // Assert
            Assert.Contains(actual.PackagesToAdd, (package) => package.Name.Equals(NewtonsoftPackageName, System.StringComparison.Ordinal));
            packageLoader.Verify(pl => pl.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
                Times.Once());
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
            var project = CreateProjectForWhichAnalyzerIsApplicable(mock);
            var packageState = await CreatePackageAnalysisState(mock, project).ConfigureAwait(false);
            var packageLoader = CreatePackageLoader(mock);

            // shift project attributes so that it is not applicable
            project.Setup(p => p.TargetFrameworks).Returns(new[] { TargetFrameworkMoniker.Net472 });
            var comparer = mock.Mock<ITargetFrameworkMonikerComparer>();
            comparer.Setup(comparer => comparer.Compare(It.IsAny<TargetFrameworkMoniker>(), It.IsAny<TargetFrameworkMoniker>()))
                .Returns(-1);

            // Act
            var actual = await analyzer.AnalyzeAsync(project.Object, packageState, default).ConfigureAwait(false);

            // Assert
            Assert.DoesNotContain(actual.PackagesToAdd, (package) => package.Name.Equals(NewtonsoftPackageName, System.StringComparison.Ordinal));
            packageLoader.Verify(pl => pl.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
                Times.Never());
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
            var project = CreateProjectForWhichAnalyzerIsApplicable(mock);
            var packageState = await CreatePackageAnalysisState(mock, project).ConfigureAwait(false);
            var packageLoader = CreatePackageLoader(mock);

            // shift project attributes so that it is not applicable
            project.Setup(p => p.OutputType).Returns(ProjectOutputType.Library);

            // Act
            var actual = await analyzer.AnalyzeAsync(project.Object, packageState, default).ConfigureAwait(false);

            // Assert
            Assert.DoesNotContain(actual.PackagesToAdd, (package) => package.Name.Equals(NewtonsoftPackageName, System.StringComparison.Ordinal));
            packageLoader.Verify(pl => pl.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
                Times.Never());
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
            var project = CreateProjectForWhichAnalyzerIsApplicable(mock);
            var packageState = await CreatePackageAnalysisState(mock, project).ConfigureAwait(false);
            var packageLoader = CreatePackageLoader(mock);

            // shift project attributes so that it is not applicable
            project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(ProjectComponents.Wpf));

            // Act
            var actual = await analyzer.AnalyzeAsync(project.Object, packageState, default).ConfigureAwait(false);

            // Assert
            Assert.DoesNotContain(actual.PackagesToAdd, (package) => package.Name.Equals(NewtonsoftPackageName, System.StringComparison.Ordinal));
            packageLoader.Verify(pl => pl.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        private static async Task<PackageAnalysisState> CreatePackageAnalysisState(AutoMock mock, Mock<IProject> project)
        {
            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.CurrentProject).Returns(project.Object);
            var restorer = mock.Mock<IPackageRestorer>();
            restorer.Setup(r => r.RestorePackagesAsync(
                It.IsAny<IUpgradeContext>(),
                It.IsAny<IProject>(),
                It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            return await PackageAnalysisState.CreateAsync(context.Object, restorer.Object, CancellationToken.None).ConfigureAwait(true);
        }

        private static Mock<IProject> CreateProjectForWhichAnalyzerIsApplicable(AutoMock mock)
        {
            var project = mock.Mock<IProject>();
            var nugetReferences = mock.Mock<INuGetReferences>();
            nugetReferences.Setup(n => n.IsTransitivelyAvailable(It.IsAny<string>()))
                .Returns(false);

            project.Setup(p => p.TargetFrameworks).Returns(new[] { TargetFrameworkMoniker.Net50 });
            project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(ProjectComponents.AspNetCore));
            project.Setup(p => p.OutputType).Returns(ProjectOutputType.Exe);
            project.Setup(p => p.GetNuGetReferencesAsync(default)).Returns(new ValueTask<INuGetReferences>(nugetReferences.Object));

            return project;
        }

        private static Mock<IPackageLoader> CreatePackageLoader(AutoMock mock)
        {
            var packageLoader = mock.Mock<IPackageLoader>();
            packageLoader.Setup(pl => pl.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult((NuGetReference?)new NuGetReference(NewtonsoftPackageName, "122.0.0")));
            return packageLoader;
        }
    }
}

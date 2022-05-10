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
    /// <summary>
    /// Unit tests for the NewtonsoftReferenceAnalyzer.
    /// </summary>
    public class NewtonsoftReferenceAnalyzerTests
    {
        private const string NewtonsoftPackageName = "Microsoft.AspNetCore.Mvc.NewtonsoftJson";
        private const string NewtonsoftPackageNameVersion = "122.0.0";

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

            var packages = new Mock<IDependencyCollection<NuGetReference>>();
            packages.As<IEnumerable<NuGetReference>>().Setup(s => s.GetEnumerator()).Returns(Enumerable.Empty<NuGetReference>().GetEnumerator());

            var packageState = new Mock<IDependencyAnalysisState>();
            packageState.Setup(p => p.Packages).Returns(packages.Object);
            packageState.Setup(p => p.TargetFrameworks).Returns(project.Object.TargetFrameworks);

            var packageLoader = CreatePackageLoader(mock);

            // Act
            await analyzer.AnalyzeAsync(project.Object, packageState.Object, default).ConfigureAwait(false);

            // Assert
            packages.Verify(p => p.Add(new NuGetReference(NewtonsoftPackageName, NewtonsoftPackageNameVersion), It.IsAny<OperationDetails>()));
            packageLoader.Verify(pl => pl.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<IEnumerable<TargetFrameworkMoniker>>(), It.IsAny<PackageSearchOptions>(), It.IsAny<CancellationToken>()),
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
            var packageState = new Mock<IDependencyAnalysisState>();
            var packageLoader = CreatePackageLoader(mock);

            // shift project attributes so that it is not applicable
            project.Setup(p => p.TargetFrameworks).Returns(new[] { TargetFrameworkMoniker.Net472 });
            var comparer = mock.Mock<ITargetFrameworkMonikerComparer>();
            comparer.Setup(comparer => comparer.Compare(It.IsAny<TargetFrameworkMoniker>(), It.IsAny<TargetFrameworkMoniker>()))
                .Returns(-1);

            packageState.Setup(p => p.TargetFrameworks).Returns(project.Object.TargetFrameworks);

            // Act
            await analyzer.AnalyzeAsync(project.Object, packageState.Object, default).ConfigureAwait(false);

            // Assert
            packageState.Verify(p => p.Packages.Add(new NuGetReference(NewtonsoftPackageName, NewtonsoftPackageNameVersion), new OperationDetails() { Risk = BuildBreakRisk.None }), Times.Never);
            packageLoader.Verify(pl => pl.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<IEnumerable<TargetFrameworkMoniker>>(), It.IsAny<PackageSearchOptions>(), It.IsAny<CancellationToken>()),
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
            var packageState = new Mock<IDependencyAnalysisState>();
            var packageLoader = CreatePackageLoader(mock);

            // shift project attributes so that it is not applicable
            project.Setup(p => p.OutputType).Returns(ProjectOutputType.Library);

            // Act
            await analyzer.AnalyzeAsync(project.Object, packageState.Object, default).ConfigureAwait(false);

            // Assert
            packageState.Verify(p => p.Packages.Add(new NuGetReference(NewtonsoftPackageName, NewtonsoftPackageNameVersion), new OperationDetails() { Risk = BuildBreakRisk.None }), Times.Never);
            packageLoader.Verify(pl => pl.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<IEnumerable<TargetFrameworkMoniker>>(), It.IsAny<PackageSearchOptions>(), It.IsAny<CancellationToken>()),
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
            var packageState = new Mock<IDependencyAnalysisState>();
            var packageLoader = CreatePackageLoader(mock);

            // shift project attributes so that it is not applicable
            project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(ProjectComponents.Wpf));

            // Act
            await analyzer.AnalyzeAsync(project.Object, packageState.Object, default).ConfigureAwait(false);

            // Assert
            packageState.Verify(p => p.Packages.Add(new NuGetReference(NewtonsoftPackageName, NewtonsoftPackageNameVersion), new OperationDetails() { Risk = BuildBreakRisk.None }), Times.Never);
            packageLoader.Verify(pl => pl.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<IEnumerable<TargetFrameworkMoniker>>(), It.IsAny<PackageSearchOptions>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        private static Mock<IProject> CreateProjectForWhichAnalyzerIsApplicable(AutoMock mock)
        {
            var tfms = new[] { TargetFrameworkMoniker.Net50 };

            var nugetReferences = new Mock<INuGetReferences>();
            nugetReferences.Setup(n => n.PackageReferences).Returns(Enumerable.Empty<NuGetReference>());

            var transitiveDependencies = mock.Mock<ITransitiveDependencyIdentifier>();
            transitiveDependencies
                .Setup(n => n.GetTransitiveDependenciesAsync(It.IsAny<IEnumerable<NuGetReference>>(), tfms, default))
                .ReturnsAsync(TransitiveClosureCollection.Empty);

            var project = new Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(tfms);
            project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(ProjectComponents.AspNetCore));
            project.Setup(p => p.OutputType).Returns(ProjectOutputType.Exe);
            project.Setup(p => p.NuGetReferences).Returns(nugetReferences.Object);

            return project;
        }

        private static Mock<IPackageLoader> CreatePackageLoader(AutoMock mock)
        {
            var packageLoader = mock.Mock<IPackageLoader>();
            packageLoader.Setup(pl => pl.GetLatestVersionAsync(It.IsAny<string>(), It.IsAny<IEnumerable<TargetFrameworkMoniker>>(), It.IsAny<PackageSearchOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult((NuGetReference?)new NuGetReference(NewtonsoftPackageName, NewtonsoftPackageNameVersion)));
            return packageLoader;
        }
    }
}

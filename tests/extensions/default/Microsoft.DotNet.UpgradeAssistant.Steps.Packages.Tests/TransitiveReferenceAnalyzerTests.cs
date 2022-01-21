// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Tests
{
    public class TransitiveReferenceAnalyzerTests
    {
        [Fact]
        public async void NoReferences()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var project = new Mock<IProject>();
            project.Setup(p => p.PackageReferences).Returns(Enumerable.Empty<NuGetReference>());

            var dependencyState = new Mock<IDependencyAnalysisState>();
            dependencyState.Setup(d => d.Packages.GetEnumerator()).Returns(Enumerable.Empty<NuGetReference>().GetEnumerator());

            // Act
            await mock.Create<TransitiveReferenceAnalyzer>().AnalyzeAsync(project.Object, dependencyState.Object, default).ConfigureAwait(false);

            // Assert
            dependencyState.Verify(d => d.Packages.Remove(It.IsAny<NuGetReference>(), It.IsAny<OperationDetails>()), Times.Never);
        }
    }
}

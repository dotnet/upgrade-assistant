// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using AutoFixture;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Web.Tests
{
    public class AspNetComponentIdentifierTests
    {
        [InlineData(new[] { "" }, ProjectComponents.None)]
        [InlineData(new[] { "Microsoft.NET.Sdk.Web" }, ProjectComponents.AspNetCore)]
        [Theory]
        public async Task SdkTypesAsync(string[] sdk, ProjectComponents expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(true);
            projectFile.Setup(f => f.Sdk).Returns(new HashSet<string>(sdk, StringComparer.OrdinalIgnoreCase));
            projectFile.Setup(f => f.GetPropertyValue(It.IsAny<string>())).Returns(string.Empty);

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.TargetFrameworks).Returns(Array.Empty<TargetFrameworkMoniker>());
            project.Setup(p => p.NuGetReferences).Returns(mock.Mock<INuGetReferences>().Object);

            var componentIdentifier = mock.Create<AspNetComponentIdentifier>();

            // Act
            var components = await componentIdentifier.GetComponentsAsync(project.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(expected, components);
        }

        [InlineData("", ProjectComponents.None)]
        [InlineData("Microsoft.AspNetCore.App", ProjectComponents.AspNetCore)]
        [Theory]
        public async Task SdkFrameworkReferencesAsync(string frameworkReference, ProjectComponents expected)
        {
            // Arrange
            const int Count = 10;
            var fixture = new Fixture();
            var frameworkReferences = fixture.CreateMany<Reference>(Count).ToList();
            frameworkReferences.Insert(Count / 2, new Reference(frameworkReference));

            using var mock = AutoMock.GetLoose();

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(true);
            projectFile.Setup(f => f.Sdk).Returns(Array.Empty<string>());
            projectFile.Setup(f => f.GetPropertyValue(It.IsAny<string>())).Returns(string.Empty);

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.FrameworkReferences).Returns(frameworkReferences);
            project.Setup(p => p.TargetFrameworks).Returns(Array.Empty<TargetFrameworkMoniker>());
            project.Setup(p => p.NuGetReferences).Returns(mock.Mock<INuGetReferences>().Object);

            var componentIdentifier = mock.Create<AspNetComponentIdentifier>();

            // Act
            var components = await componentIdentifier.GetComponentsAsync(project.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(expected, components);
        }

        [InlineData("", ProjectComponents.None)]
        [InlineData("System.Web", ProjectComponents.AspNet)]
        [InlineData("System.Web.Abstractions", ProjectComponents.AspNet)]
        [InlineData("System.Web.Routing", ProjectComponents.AspNet)]
        [Theory]
        public async Task NonSdkReferencesAsync(string reference, ProjectComponents expected)
        {
            // Arrange
            const int Count = 10;
            var fixture = new Fixture();
            var references = fixture.CreateMany<Reference>(Count).ToList();
            references.Insert(Count / 2, new Reference(reference));

            using var mock = AutoMock.GetLoose();

            var projectFile = mock.Mock<IProjectFile>();

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.References).Returns(references);
            project.Setup(p => p.TargetFrameworks).Returns(Array.Empty<TargetFrameworkMoniker>());
            project.Setup(p => p.NuGetReferences).Returns(mock.Mock<INuGetReferences>().Object);

            var componentIdentifier = mock.Create<AspNetComponentIdentifier>();

            // Act
            var components = await componentIdentifier.GetComponentsAsync(project.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(expected, components);
        }

        [InlineData("", ProjectComponents.None)]
        [InlineData("Microsoft.WebApplication.targets", ProjectComponents.AspNet)]
        [Theory]
        public async Task NonSdkImports(string import, ProjectComponents expected)
        {
            // Arrange
            const int Count = 10;
            var fixture = new Fixture();
            var imports = fixture.CreateMany<string>(Count).ToList();
            imports.Insert(Count / 2, import);

            using var mock = AutoMock.GetLoose();

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(false);
            projectFile.Setup(p => p.Imports).Returns(imports);

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.TargetFrameworks).Returns(Array.Empty<TargetFrameworkMoniker>());
            project.Setup(p => p.NuGetReferences).Returns(mock.Mock<INuGetReferences>().Object);

            var componentIdentifier = mock.Create<AspNetComponentIdentifier>();

            // Act
            var components = await componentIdentifier.GetComponentsAsync(project.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(expected, components);
        }
    }
}

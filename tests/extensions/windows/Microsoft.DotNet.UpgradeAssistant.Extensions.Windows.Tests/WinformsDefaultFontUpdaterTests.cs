// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.Tests
{
    public class WinformsDefaultFontUpdaterTests
    {
        [InlineData(ProjectComponents.Wpf, false)]
        [InlineData(ProjectComponents.WinForms, true)]
        [InlineData(ProjectComponents.None, false)]
        [InlineData(ProjectComponents.AspNet | ProjectComponents.WindowsDesktop, false)]
        [InlineData(ProjectComponents.WinForms | ProjectComponents.WindowsDesktop, true)]
        [Theory]
        public async Task IsApplicableTests(ProjectComponents component, bool expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var logger = mock.Mock<ILogger<WinformsDefaultFontUpdater>>();
            var updater = new WinformsDefaultFontUpdater(logger.Object);

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(true);

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.FileInfo).Returns(new FileInfo("./test"));
            project.Setup(p => p.GetComponentsAsync(CancellationToken.None)).ReturnsAsync(component);

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project.Object });

            // Act
            var updaterResult = await updater.IsApplicableAsync(context.Object, ImmutableArray<IProject>.Empty.Add(project.Object), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(expected, updaterResult.Result);
        }

        [InlineData(ProjectComponents.Wpf, ProjectComponents.WinForms, true, new string[] { "test2" })]
        [InlineData(ProjectComponents.WinForms, ProjectComponents.WinForms, true, new string[] { "test1", "test2" })]
        [InlineData(ProjectComponents.Wpf, ProjectComponents.WindowsDesktop, false, new string[] { })]
        [InlineData(ProjectComponents.None, ProjectComponents.WinForms, true, new string[] { "test2" })]
        [Theory]
        public async Task IsApplicableMultiProjectTests(ProjectComponents component1, ProjectComponents component2, bool expected, string[] expectedFileLocations)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var logger = mock.Mock<ILogger<WinformsDefaultFontUpdater>>();
            var updater = new WinformsDefaultFontUpdater(logger.Object);

            var projectFile1 = new Mock<IProjectFile>();
            projectFile1.Setup(f => f.IsSdk).Returns(true);

            var projectFile2 = new Mock<IProjectFile>();
            projectFile2.Setup(f => f.IsSdk).Returns(true);

            var project1 = new Mock<IProject>();
            project1.Setup(p => p.GetFile()).Returns(projectFile1.Object);
            project1.Setup(p => p.FileInfo).Returns(new FileInfo("./test1"));
            project1.Setup(p => p.GetComponentsAsync(CancellationToken.None)).ReturnsAsync(component1);

            var project2 = new Mock<IProject>();
            project2.Setup(p => p.GetFile()).Returns(projectFile2.Object);
            project2.Setup(p => p.FileInfo).Returns(new FileInfo("./test2"));
            project2.Setup(p => p.GetComponentsAsync(CancellationToken.None)).ReturnsAsync(component2);

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project1.Object, project2.Object });

            // Act
            var updaterResult = (WindowsDesktopUpdaterResult)await updater.IsApplicableAsync(context.Object, context.Object.Projects.ToImmutableArray(), CancellationToken.None).ConfigureAwait(false);
            var fileLocations = updaterResult.FileLocations.Select(i => Path.GetFileNameWithoutExtension(i));

            // Assert
            Assert.Equal(expected, updaterResult.Result);
            Assert.Equal(expectedFileLocations, fileLocations);
        }
    }
}

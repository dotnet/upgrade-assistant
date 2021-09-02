// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Moq;
using Xunit;

using static Microsoft.DotNet.UpgradeAssistant.TargetFrameworkMonikerParser;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui.Tests
{
    public class MauiComponentIdentificationTests
    {
        [InlineData(new string[] { }, ProjectComponents.None)]
        [InlineData(new[] { NetStandard20, NetStandard21 }, ProjectComponents.None)]
        [InlineData(new[] { Net60_Android }, ProjectComponents.MauiAndroid)]
        [InlineData(new[] { Net60_Windows }, ProjectComponents.None)]
        [InlineData(new[] { Net60_iOS }, ProjectComponents.MauiiOS)]
        [InlineData(new[] { Net60_Android, Net60_iOS }, ProjectComponents.MauiAndroid | ProjectComponents.MauiiOS)]
        [InlineData(new[] { NetStandard20, Net60_Android, Net60_iOS }, ProjectComponents.MauiAndroid | ProjectComponents.MauiiOS)]
        [Theory]
        public async Task ComponentIdentificationByTfmAsync(string[] tfms, ProjectComponents expectedComponents)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var projectFile = new Mock<IProjectFile>();

            var project = new Mock<IProject>();
            project.Setup(p => p.GetProjectPropertyElements()).Returns(new Mock<IProjectPropertyElements>().Object);
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.TargetFrameworks).Returns(tfms.Select(ParseTfm).ToArray()!);
            project.Setup(p => p.NuGetReferences).Returns(new Mock<INuGetReferences>().Object);

            // Act
            var components = await mock.Create<MauiComponentIdentifier>().GetComponentsAsync(project.Object, default);

            // Assert
            Assert.Equal(expectedComponents, components);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.DotNet.UpgradeAssistant.TargetFramework;
using Moq;
using Xunit;

using static Microsoft.DotNet.UpgradeAssistant.TargetFrameworkMonikerParser;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class WindowsSdkTargetFrameworkSelectorFilterTests
    {
        [InlineData(ProjectComponents.None, ProjectOutputType.Exe, Net50, Net50, false)]
        [InlineData(ProjectComponents.AspNet, ProjectOutputType.Exe, Net50, Net50, false)]
        [InlineData(ProjectComponents.AspNetCore, ProjectOutputType.Exe, Net50, Net50, false)]
        [InlineData(ProjectComponents.WindowsDesktop, ProjectOutputType.Exe, Net50, Net50_Windows, true)]
        [InlineData(ProjectComponents.None, ProjectOutputType.WinExe, Net50, Net50_Windows, true)]
        [InlineData(ProjectComponents.AspNet | ProjectComponents.WindowsDesktop, ProjectOutputType.Exe, Net50, Net50_Windows, true)]
        [InlineData(ProjectComponents.WinForms | ProjectComponents.WindowsDesktop, ProjectOutputType.Exe, Net50, Net50_Windows, true)]
        [InlineData(ProjectComponents.WinRT | ProjectComponents.WindowsDesktop, ProjectOutputType.Exe, Net50, Net50_Windows_10_0_19041_0, true)]
        [Theory]
        public void ProcessTests(ProjectComponents components, ProjectOutputType outputType, string startingTfmString, string expectedTfmString, bool tryUpdate)
        {
            // Arrange
            var fixture = new Fixture();
            using var mock = AutoMock.GetLoose();

            var tfm = ParseTfm(startingTfmString);
            var expectedTfm = ParseTfm(expectedTfmString);

            var project = new Mock<IProject>();
            project.Setup(p => p.OutputType).Returns(outputType);

            var state = new Mock<ITargetFrameworkSelectorFilterState>();
            state.Setup(s => s.Current).Returns(tfm);
            state.Setup(s => s.Project).Returns(project.Object);
            state.Setup(s => s.Components).Returns(components);

            var filter = mock.Create<WindowsSdkTargetFrameworkSelectorFilter>();

            // Act
            filter.Process(state.Object);

            // Assert
            var count = tryUpdate ? Times.Once() : Times.Never();
            state.Verify(s => s.TryUpdate(expectedTfm), count);
        }
    }
}

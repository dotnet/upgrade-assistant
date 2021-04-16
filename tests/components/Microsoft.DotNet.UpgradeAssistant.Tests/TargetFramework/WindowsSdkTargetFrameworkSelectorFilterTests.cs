// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.DotNet.UpgradeAssistant.TargetFramework;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class WindowsSdkTargetFrameworkSelectorFilterTests
    {
        [InlineData(ProjectComponents.None, ProjectOutputType.Exe, "", false)]
        [InlineData(ProjectComponents.AspNet, ProjectOutputType.Exe, "", false)]
        [InlineData(ProjectComponents.AspNetCore, ProjectOutputType.Exe, "", false)]
        [InlineData(ProjectComponents.WindowsDesktop, ProjectOutputType.Exe, "-windows", true)]
        [InlineData(ProjectComponents.None, ProjectOutputType.WinExe, "-windows", true)]
        [InlineData(ProjectComponents.AspNet | ProjectComponents.WindowsDesktop, ProjectOutputType.Exe, "-windows", true)]
        [InlineData(ProjectComponents.WinForms | ProjectComponents.WindowsDesktop, ProjectOutputType.Exe, "-windows", true)]
        [InlineData(ProjectComponents.WinRT | ProjectComponents.WindowsDesktop, ProjectOutputType.Exe, "-windows10.0.19041.0", true)]
        [Theory]
        public void ProcessTests(ProjectComponents components, ProjectOutputType outputType, string expectedSuffix, bool tryUpdate)
        {
            // Arrange
            var fixture = new Fixture();
            using var mock = AutoMock.GetLoose();

            var tfm = fixture.Create<TargetFrameworkMoniker>();
            var expectedTfm = new TargetFrameworkMoniker(tfm.Name + expectedSuffix);

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

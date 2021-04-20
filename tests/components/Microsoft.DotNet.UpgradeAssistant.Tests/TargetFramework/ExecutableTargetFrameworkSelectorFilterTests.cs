// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.DotNet.UpgradeAssistant.TargetFramework;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class ExecutableTargetFrameworkSelectorFilterTests
    {
        [InlineData(ProjectOutputType.Exe, true)]
        [InlineData(ProjectOutputType.Library, false)]
        [InlineData(ProjectOutputType.Other, false)]
        [InlineData(ProjectOutputType.WinExe, false)]
        [Theory]
        public void ProcessTests(ProjectOutputType outputType, bool tryUpdate)
        {
            // Arrange
            var fixture = new Fixture();
            using var mock = AutoMock.GetLoose();

            var project = new Mock<IProject>();
            project.Setup(p => p.OutputType).Returns(outputType);

            var tfm = fixture.Create<TargetFrameworkMoniker>();

            var state = new Mock<ITargetFrameworkSelectorFilterState>();
            state.Setup(s => s.Project).Returns(project.Object);
            state.Setup(s => s.AppBase).Returns(tfm);

            var filter = mock.Create<ExecutableTargetFrameworkSelectorFilter>();

            // Act
            filter.Process(state.Object);

            // Assert
            var count = tryUpdate ? Times.Once() : Times.Never();
            state.Verify(s => s.TryUpdate(tfm), count);
        }
    }
}

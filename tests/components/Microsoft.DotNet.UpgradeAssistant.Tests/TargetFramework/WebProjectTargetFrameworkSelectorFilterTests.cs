// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.DotNet.UpgradeAssistant.TargetFramework;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class WebProjectTargetFrameworkSelectorFilterTests
    {
        [InlineData(ProjectComponents.None, false)]
        [InlineData(ProjectComponents.AspNet, true)]
        [InlineData(ProjectComponents.AspNetCore, true)]
        [InlineData(ProjectComponents.WindowsDesktop, false)]
        [InlineData(ProjectComponents.AspNet | ProjectComponents.WindowsDesktop, true)]
        [InlineData(ProjectComponents.WinForms, false)]
        [Theory]
        public void ProcessTests(ProjectComponents components, bool tryUpdate)
        {
            // Arrange
            var fixture = new Fixture();
            using var mock = AutoMock.GetLoose();

            var tfm = fixture.Create<TargetFrameworkMoniker>();

            var state = new Mock<ITargetFrameworkSelectorFilterState>();
            state.Setup(s => s.Components).Returns(components);
            state.Setup(s => s.AppBase).Returns(tfm);

            var filter = mock.Create<WebProjectTargetFrameworkSelectorFilter>();

            // Act
            filter.Process(state.Object);

            // Assert
            var count = tryUpdate ? Times.Once() : Times.Never();
            state.Verify(s => s.TryUpdate(tfm), count);
        }
    }
}

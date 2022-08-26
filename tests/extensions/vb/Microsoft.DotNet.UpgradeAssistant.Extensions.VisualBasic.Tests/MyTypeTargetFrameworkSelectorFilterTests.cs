// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Autofac.Extras.Moq;

using Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic;

using Moq;

using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.VisualBasic.Tests
{
    public class MyTypeTargetFrameworkSelectorFilterTests
    {
        [InlineData("Windows", true)]
        [InlineData("WindowsForms", true)]
        [InlineData("Other", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [Theory]
        public void ProcessTests(string myType, bool update)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var state = new Mock<ITargetFrameworkSelectorFilterState>();
            state.Setup(s => s.AppBase).Returns(TargetFrameworkMoniker.Net60);

            var project = new Mock<IProject>();
            state.Setup(s => s.Project).Returns(project.Object);

            var file = new Mock<IProjectFile>();
            project.Setup(p => p.GetFile()).Returns(file.Object);

            file.Setup(f => f.GetPropertyValue("MyType")).Returns(myType);

            // Act
            mock.Create<MyTypeTargetFrameworkSelectorFilter>().Process(state.Object);

            // Assert
            var expectedTimes = update ? Times.Once() : Times.Never();
            state.Verify(s => s.TryUpdate(TargetFrameworkMoniker.Net60_Windows), expectedTimes);
        }
    }
}

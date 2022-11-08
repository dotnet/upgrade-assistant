// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Autofac.Extras.Moq;
using AutoFixture;
using Moq;
using Xunit;

using static Microsoft.DotNet.UpgradeAssistant.TargetFrameworkMonikerParser;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui.Tests
{
    public class MauiProjectTargetFrameworkSelectorFilterTests
    {
        [InlineData(ProjectComponents.XamarinAndroid, Net70_Android, true)]
        [InlineData(ProjectComponents.XamariniOS, Net70_iOS, true)]
        [InlineData(ProjectComponents.MauiAndroid, Net70_Android, true)]
        [InlineData(ProjectComponents.MauiiOS, Net70_iOS, true)]
        [InlineData(ProjectComponents.Maui, Net70_Android, true)]
        [Theory]
        public void ProcessTests(ProjectComponents components, string expectedTfmString, bool tryUpdate)
        {
            // Arrange
            var fixture = new Fixture();
            using var mock = AutoMock.GetLoose();

            var expectedTfm = ParseTfm(expectedTfmString);

            var state = new Mock<ITargetFrameworkSelectorFilterState>();
            state.Setup(s => s.Components).Returns(components);

            var filter = mock.Create<MauiTargetFrameworkSelectorFilter>();

            // Act
            filter.Process(state.Object);

            // Assert
            var count = tryUpdate ? Times.Once() : Times.Never();
            state.Verify(s => s.TryUpdate(expectedTfm), count);
        }
    }
}

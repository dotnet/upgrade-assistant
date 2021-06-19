// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.DotNet.UpgradeAssistant.Checks;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests.Checks
{
    public class MultiTargetingCheckTests
    {
        [InlineData(0, UpgradeReadiness.NotReady)]
        [InlineData(1, UpgradeReadiness.Ready)]
        [InlineData(2, UpgradeReadiness.NotReady)]
        [InlineData(3, UpgradeReadiness.NotReady)]
        [Theory]
        public async Task IsReadyTest(int tfmCount, UpgradeReadiness readiness)
        {
            // Arrange
            var fixture = new Fixture();
            var tfms = fixture.CreateMany<TargetFrameworkMoniker>(tfmCount).ToArray();

            using var mock = AutoMock.GetLoose();
            var readyCheck = mock.Create<MultiTargetFrameworkCheck>();

            var project = new Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(tfms);

            var options = mock.Mock<UpgradeReadinessOptions>();

            // Act
            var result = await readyCheck.IsReadyAsync(project.Object, options.Object, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(readiness, result);
        }
    }
}

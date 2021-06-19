// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Checks.Tests
{
    public class CentralPackageManagementCheckTests
    {
        [InlineData("true", UpgradeReadiness.NotReady)]
        [InlineData("True", UpgradeReadiness.NotReady)]
        [InlineData("false", UpgradeReadiness.Ready)]
        [InlineData("False", UpgradeReadiness.Ready)]
        [InlineData(null, UpgradeReadiness.Ready)]
        [InlineData("", UpgradeReadiness.Ready)]
        [Theory]
        public async Task ChecksProperty(string isCentrallyManaged, UpgradeReadiness isReady)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var file = mock.Mock<IProjectFile>();
            file.Setup(f => f.GetPropertyValue("EnableCentralPackageVersions")).Returns(isCentrallyManaged);

            var project = mock.Mock<IProject>();
            project.Setup(f => f.GetFile()).Returns(file.Object);

            var options = mock.Mock<UpgradeReadinessOptions>();

            // Act
            var result = await mock.Create<CentralPackageManagementCheck>().IsReadyAsync(project.Object, options.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(isReady, result);
        }
    }
}

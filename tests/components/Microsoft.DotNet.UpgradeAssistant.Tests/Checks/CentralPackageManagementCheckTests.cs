// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Checks.Tests
{
    public class CentralPackageManagementCheckTests
    {
        [InlineData("true", false)]
        [InlineData("True", false)]
        [InlineData("false", true)]
        [InlineData("False", true)]
        [InlineData(null, true)]
        [InlineData("", true)]
        [Theory]
        public async Task ChecksProperty(string isCentrallyManaged, bool isReady)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var file = mock.Mock<IProjectFile>();
            file.Setup(f => f.GetPropertyValue("EnableCentralPackageVersions")).Returns(isCentrallyManaged);

            var project = mock.Mock<IProject>();
            project.Setup(f => f.GetFile()).Returns(file.Object);

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(m => m.Projects).Returns(new[] { project.Object });

            // Act
            var result = await mock.Create<CentralPackageManagementCheck>().IsReadyAsync(context.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(isReady, result);
        }
    }
}

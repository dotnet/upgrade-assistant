// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Tests
{
    public class ExtensionManagerTests
    {
        private readonly Fixture _fixture;

        public ExtensionManagerTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public async Task AddExtensionTestIsAdded()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var options = _fixture.Create<ExtensionOptions>();
            var extension = _fixture.Create<ExtensionSource>();
            var config = new UpgradeAssistantConfiguration();
            var expected = config with { Extensions = ImmutableArray.Create(extension) };

            mock.Mock<IUpgradeAssistantConfigurationLoader>().Setup(l => l.LoadAsync(default)).ReturnsAsync(config);
            mock.Mock<IOptions<ExtensionOptions>>().Setup(l => l.Value).Returns(options);

            // Act
            await mock.Create<ExtensionManager>().AddAsync(extension, default).ConfigureAwait(false);

            // Assert
            mock.Mock<IUpgradeAssistantConfigurationLoader>().Setup(l => l.SaveAsync(expected, default));
        }

        [Fact]
        public async Task RemoveExtensionIsRemoved()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var extension = _fixture.Create<ExtensionSource>();
            var config = new UpgradeAssistantConfiguration { Extensions = ImmutableArray.Create(extension) };
            var expected = config with { Extensions = config.Extensions.Remove(extension) };

            mock.Mock<IUpgradeAssistantConfigurationLoader>().Setup(l => l.LoadAsync(default)).ReturnsAsync(config);

            // Act
            var result = await mock.Create<ExtensionManager>().RemoveAsync(extension.Name, default).ConfigureAwait(false);

            // Assert
            Assert.True(result);
            mock.Mock<IUpgradeAssistantConfigurationLoader>().Setup(l => l.SaveAsync(expected, default));
        }

        [Fact]
        public async Task UpdateIsNotUpdated()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var extension = _fixture.Create<ExtensionSource>();
            var config = new UpgradeAssistantConfiguration { Extensions = ImmutableArray.Create(extension) };
            var expected = config with { Extensions = config.Extensions.Remove(extension) };

            mock.Mock<IUpgradeAssistantConfigurationLoader>().Setup(l => l.LoadAsync(default)).ReturnsAsync(config);

            // Act
            var result = await mock.Create<ExtensionManager>().UpdateAsync(extension.Name, default).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
            mock.Mock<IUpgradeAssistantConfigurationLoader>().Setup(l => l.SaveAsync(expected, default));
        }
    }
}

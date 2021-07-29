// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            var restoredPath = _fixture.Create<string>();
            var config = new UpgradeAssistantConfiguration();
            var expected = config with { Extensions = ImmutableArray.Create(extension) };

            mock.Mock<IOptions<ExtensionOptions>>().Setup(l => l.Value).Returns(options);
            mock.Mock<IUpgradeAssistantConfigurationLoader>().Setup(l => l.Load()).Returns(config);
            mock.Mock<IExtensionDownloader>().Setup(l => l.RestoreAsync(extension, default)).ReturnsAsync(restoredPath);

            // Act
            await mock.Create<ExtensionManager>().AddAsync(extension, default).ConfigureAwait(false);

            // Assert
            mock.Mock<IUpgradeAssistantConfigurationLoader>().Verify(l => l.Save(Match(expected)), Times.Once);
        }

        [Fact]
        public async Task RemoveExtensionIsRemoved()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var extension = _fixture.Create<ExtensionSource>();
            var config = new UpgradeAssistantConfiguration { Extensions = ImmutableArray.Create(extension) };
            var expected = config with { Extensions = config.Extensions.Remove(extension) };

            mock.Mock<IUpgradeAssistantConfigurationLoader>().Setup(l => l.Load()).Returns(config);

            // Act
            var result = await mock.Create<ExtensionManager>().RemoveAsync(extension.Name, default).ConfigureAwait(false);

            // Assert
            Assert.True(result);
            mock.Mock<IUpgradeAssistantConfigurationLoader>().Verify(l => l.Save(Match(expected)), Times.Once);
        }

        [Fact]
        public async Task UpdateIsUpdated()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var extension = _fixture.Create<ExtensionSource>();
            var config = new UpgradeAssistantConfiguration { Extensions = ImmutableArray.Create(extension) };
            var latestVersion = _fixture.Create<Version>().ToString();
            var latestSource = extension with { Version = latestVersion };
            var expected = config with { Extensions = ImmutableArray.Create(latestSource) };

            mock.Mock<IUpgradeAssistantConfigurationLoader>().Setup(l => l.Load()).Returns(config);
            mock.Mock<IExtensionDownloader>().Setup(l => l.GetLatestVersionAsync(It.Is<ExtensionSource>(e => e.Name == extension.Name), default)).ReturnsAsync(latestVersion);

            // Act
            var result = await mock.Create<ExtensionManager>().UpdateAsync(extension.Name, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(latestSource, result);
            mock.Mock<IUpgradeAssistantConfigurationLoader>().Verify(l => l.Save(Match(expected)), Times.Once);
        }

        private static UpgradeAssistantConfiguration Match(UpgradeAssistantConfiguration expected)
            => It.Is<UpgradeAssistantConfiguration>(actual => Compare(actual, expected));

        private static bool Compare(UpgradeAssistantConfiguration actual, UpgradeAssistantConfiguration expected)
            => actual.Extensions.SequenceEqual(expected.Extensions, EqualityComparer<ExtensionSource>.Default);
    }
}

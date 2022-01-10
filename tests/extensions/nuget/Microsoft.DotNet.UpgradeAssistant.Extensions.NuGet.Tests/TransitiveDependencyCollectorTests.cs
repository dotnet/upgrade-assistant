// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet.Tests
{
    public class TransitiveDependencyCheckerTests
    {
        private readonly Fixture _fixture;

        public TransitiveDependencyCheckerTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public async Task NoDependencies()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var collector = mock.Create<TransitiveDependencyChecker>();
            var tfm = _fixture.Create<TargetFrameworkMoniker>();
            var packages = _fixture.CreateMany<NuGetReference>();

            var package = new NuGetReference(_fixture.Create<string>(), "1.0.0");

            mock.Mock<ITransitiveDependencyCollector>().Setup(c => c.GetTransitiveDependenciesAsync(packages, tfm, default)).ReturnsAsync(Array.Empty<NuGetReference>());

            // Act
            var result = await collector.IsTransitiveDependencyAsync(packages, package, tfm, default).ConfigureAwait(false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DependencyContainsExactMatch()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var collector = mock.Create<TransitiveDependencyChecker>();
            var tfm = _fixture.Create<TargetFrameworkMoniker>();
            var packages = _fixture.CreateMany<NuGetReference>();

            var package = new NuGetReference(_fixture.Create<string>(), "1.0.0");
            var dependencies = new[]
            {
                new NuGetReference(package.Name, "1.0.0"),
                new NuGetReference(_fixture.Create<string>(), "2.0.0"),
            };

            mock.Mock<ITransitiveDependencyCollector>().Setup(c => c.GetTransitiveDependenciesAsync(packages, tfm, default)).ReturnsAsync(dependencies);

            // Act
            var result = await collector.IsTransitiveDependencyAsync(packages, package, tfm, default).ConfigureAwait(false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DependencyContainsExactMatchAndLater()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var collector = mock.Create<TransitiveDependencyChecker>();
            var tfm = _fixture.Create<TargetFrameworkMoniker>();
            var packages = _fixture.CreateMany<NuGetReference>();

            var package = new NuGetReference(_fixture.Create<string>(), "1.0.0");
            var dependencies = new[]
            {
                new NuGetReference(package.Name, "1.0.0"),
                new NuGetReference(package.Name, "1.0.1"),
                new NuGetReference(_fixture.Create<string>(), "2.0.0"),
            };

            mock.Mock<ITransitiveDependencyCollector>().Setup(c => c.GetTransitiveDependenciesAsync(packages, tfm, default)).ReturnsAsync(dependencies);

            // Act
            var result = await collector.IsTransitiveDependencyAsync(packages, package, tfm, default).ConfigureAwait(false);

            // Assert
            Assert.True(result);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Tests
{
    public class ExtensionConfigurationTests
    {
        private readonly Fixture _fixture;

        public ExtensionConfigurationTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void ItemsSet()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            // Arrange
            var expected = _fixture.Create<Option1>();
            var manifests = new[]
            {
                new Option1 { A = expected.A },
                new Option1 { B = expected.B },
            };

            using var provider = CreateServiceProvider(manifests);

            // Act
            var options = provider.GetRequiredService<IOptions<Option1>>();

            // Assert
            Assert.Equal(expected, options.Value);
        }

        [Fact]
        public void SingleArray()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            // Arrange
            var manifests = new[]
            {
                new OptionsWithArray { Array = new[] { 1, 2 } },
            };

            using var provider = CreateServiceProvider(manifests);

            // Act
            var options = provider.GetRequiredService<IOptions<OptionsWithArray>>();

            // Assert
            Assert.Equal(new[] { 1, 2 }, options.Value.Array);
        }

        [Fact]
        public void LastArrayIsUsed()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            // Arrange
            var manifests = new[]
            {
                new OptionsWithArray { Array = new[] { 1, 2 } },
                new OptionsWithArray { Array = new[] { 3, 4 } },
            };

            using var provider = CreateServiceProvider(manifests);

            // Act
            var options = provider.GetRequiredService<IOptions<OptionsWithArray>>();

            // Assert
            Assert.Equal(new[] { 3, 4 }, options.Value.Array);
        }

        [Fact]
        public void CanGetAllArrays()
        {
            // Arrange
            var manifests = new[]
            {
                new OptionsWithArray { Array = new[] { 1, 2 } },
                new OptionsWithArray { Array = new[] { 3, 4 } },
            };

            using var provider = CreateServiceProvider(manifests);

            // Act
            var options = provider.GetRequiredService<IOptions<ICollection<OptionsWithArray>>>();

            // Assert
            Assert.Collection(
                options.Value,
                c => Assert.Equal(manifests[0].Array, c.Array),
                c => Assert.Equal(manifests[1].Array, c.Array));
        }

        private static ServiceProvider CreateServiceProvider<T>(IEnumerable<T> manifests)
            where T : class, new()
        {
            var services = new ServiceCollection();
            var builder = new ExtensionServiceCollection(services, null!);

            builder.AddExtensionOption<T>(nameof(Section<T>.Manifest));

            var extensions = manifests.Select(CreateExtension).ToList();

            var extensionManager = new Mock<IExtensionProvider>();
            extensionManager.Setup(m => m.Instances).Returns(extensions);

            services.AddSingleton(extensionManager.Object);

            return services.BuildServiceProvider();

            static ExtensionInstance CreateExtension(T manifest)
            {
                var strManifest = JsonSerializer.Serialize(new Section<T>(manifest));
                var fileProvider = new Mock<IFileProvider>();

                var file = new Mock<IFileInfo>();
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(strManifest));
                file.Setup(f => f.Name).Returns(ExtensionInstance.ManifestFileName);
                file.Setup(f => f.CreateReadStream()).Returns(ms);
                file.Setup(f => f.Exists).Returns(true);

                fileProvider.Setup(f => f.GetFileInfo(ExtensionInstance.ManifestFileName)).Returns(file.Object);

                var factory = new ExtensionInstanceFactory(new Mock<ILogger<ExtensionInstance>>().Object);

                return factory.Create(fileProvider.Object, string.Empty);
            }
        }

        private class Section<T>
        {
            public Section(T manifest)
            {
                Manifest = manifest;
            }

            public T Manifest { get; }
        }

        private record OptionsWithArray
        {
            public int[] Array { get; set; } = null!;
        }

        private record Option1
        {
            public string? A { get; set; }

            public string? B { get; set; }
        }
    }
}

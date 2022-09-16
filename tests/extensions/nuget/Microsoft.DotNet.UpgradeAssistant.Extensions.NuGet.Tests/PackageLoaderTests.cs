// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet.Tests
{
    public class PackageLoaderTests
    {
        private readonly Fixture _fixture;

        public PackageLoaderTests()
        {
            _fixture = new Fixture();

            _fixture.Customize<NuGetReference>(o =>
                o.Without(o => o.PrivateAssets)
                 .Without(o => o.Version));
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void FilterTestEmptyInput(bool isNull)
        {
            // Arrange
            var name = _fixture.Create<string>();
            var searchResults = isNull ? null : Array.Empty<IPackageSearchMetadata>();
            var tfms = _fixture.CreateMany<TargetFrameworkMoniker>();
            var loader = GetDefaultPackageLoader();

            // Act
            // Convert to enumerable since xunit doesn't have great support for IAsyncEnumerable yet
            var result = loader.FilterSearchResultsAsync(name, searchResults!, tfms).ToEnumerable();

            // Assert
            Assert.Empty(result);
        }

        private static IPackageSearchMetadata MockSearchMetadata(NuGetReference reference, params TargetFrameworkMoniker[] tfms)
        {
            var metadata = new Mock<IPackageSearchMetadata>();
            var identity = new PackageIdentity(reference.Name, reference.GetNuGetVersion());
            var groups = tfms.Select(tfm => new PackageDependencyGroup(NuGetFramework.Parse(tfm.Name), Enumerable.Empty<PackageDependency>()));

            metadata.Setup(m => m.Identity).Returns(identity);
            metadata.Setup(m => m.DependencySets).Returns(groups);

            return metadata.Object;
        }

        [Fact]
        public void FilterExplicitMatch()
        {
            // Arrange
            var item = _fixture.Create<NuGetReference>() with { Version = "1.0.0" };
            var tfm = TargetFrameworkMoniker.NetStandard20;
            var metadata = MockSearchMetadata(item, tfm);
            var loader = GetDefaultPackageLoader();

            // Act
            // Convert to enumerable since xunit doesn't have great support for IAsyncEnumerable yet
            var result = loader.FilterSearchResultsAsync(item.Name, new[] { metadata }, new[] { tfm }).ToEnumerable();

            // Assert
            Assert.Collection(result, r => Assert.Equal(r, item));
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void FilterSupported(bool isBackwards)
        {
            // Arrange
            var item1 = _fixture.Create<NuGetReference>() with { Version = "1.0.0" };
            var item2 = item1 with { Version = "2.0.0" };
            var metadata1 = MockSearchMetadata(item1, TargetFrameworkMoniker.NetStandard20);
            var metadata2 = MockSearchMetadata(item2, TargetFrameworkMoniker.Net50);
            var loader = GetDefaultPackageLoader();

            var list = isBackwards ? new[] { metadata2, metadata1 } : new[] { metadata1, metadata2 };

            // Act
            // Convert to enumerable since xunit doesn't have great support for IAsyncEnumerable yet
            var result = loader.FilterSearchResultsAsync(item1.Name, list, new[] { TargetFrameworkMoniker.Net50 }).ToEnumerable();

            // Assert
            Assert.Collection(result,
                r => Assert.Equal(r, item1),
                r => Assert.Equal(r, item2));
        }

        [Fact]
        public void Filter3Only2Supported()
        {
            // Arrange
            var item1 = _fixture.Create<NuGetReference>() with { Version = "1.0.0" };
            var item2 = item1 with { Version = "2.0.0" };
            var item3 = item1 with { Version = "3.0.0" };

            var metadata1 = MockSearchMetadata(item1, TargetFrameworkMoniker.NetStandard20);
            var metadata2 = MockSearchMetadata(item2, TargetFrameworkMoniker.Net50);
            var metadata3 = MockSearchMetadata(item3, TargetFrameworkMoniker.Net60);

            var loader = GetDefaultPackageLoader();

            foreach (var metadata in Permute(metadata1, metadata2, metadata3))
            {
                // Act
                // Convert to enumerable since xunit doesn't have great support for IAsyncEnumerable yet
                var result = loader.FilterSearchResultsAsync(item1.Name, metadata, new[] { TargetFrameworkMoniker.Net50 }).ToEnumerable();

                // Assert
                Assert.Collection(result,
                    r => Assert.Equal(r, item1),
                    r => Assert.Equal(r, item2));
            }
        }

        [Fact]
        public void Filter3Only1Supported()
        {
            // Arrange
            var item1 = _fixture.Create<NuGetReference>() with { Version = "1.0.0" };
            var item2 = item1 with { Version = "2.0.0" };
            var item3 = item1 with { Version = "3.0.0" };

            var metadata1 = MockSearchMetadata(item1, TargetFrameworkMoniker.Net45);
            var metadata2 = MockSearchMetadata(item2, TargetFrameworkMoniker.Net50);
            var metadata3 = MockSearchMetadata(item3, TargetFrameworkMoniker.Net60);

            var loader = GetDefaultPackageLoader();

            foreach (var metadata in Permute(metadata1, metadata2, metadata3))
            {
                // Act
                // Convert to enumerable since xunit doesn't have great support for IAsyncEnumerable yet
                var result = loader.FilterSearchResultsAsync(item1.Name, metadata, new[] { TargetFrameworkMoniker.Net50 }).ToEnumerable();

                // Assert
                Assert.Collection(result, r => Assert.Equal(r, item2));
            }
        }

        [Fact]
        public void Filter3Only1SupportedMultipleTfmsInSearch()
        {
            // Arrange
            var item1 = _fixture.Create<NuGetReference>() with { Version = "1.0.0" };
            var item2 = item1 with { Version = "2.0.0" };
            var item3 = item1 with { Version = "3.0.0" };

            var metadata1 = MockSearchMetadata(item1, TargetFrameworkMoniker.NetStandard10, TargetFrameworkMoniker.NetStandard20);
            var metadata2 = MockSearchMetadata(item2, TargetFrameworkMoniker.Net45, TargetFrameworkMoniker.Net50);
            var metadata3 = MockSearchMetadata(item3, TargetFrameworkMoniker.Net50_Linux, TargetFrameworkMoniker.Net50_Windows);

            var loader = GetDefaultPackageLoader();

            foreach (var metadata in Permute(metadata1, metadata2, metadata3))
            {
                // Act
                // Convert to enumerable since xunit doesn't have great support for IAsyncEnumerable yet
                var result = loader.FilterSearchResultsAsync(item1.Name, metadata, new[] { TargetFrameworkMoniker.Net50 }).ToEnumerable();

                // Assert
                Assert.Collection(result,
                    r => Assert.Equal(r, item1),
                    r => Assert.Equal(r, item2));
            }
        }

        [Fact]
        public void Filter3Only1SupportedMultipleTfms()
        {
            // Arrange
            var item1 = _fixture.Create<NuGetReference>() with { Version = "1.0.0" };
            var item2 = item1 with { Version = "2.0.0" };
            var item3 = item1 with { Version = "3.0.0" };

            var metadata1 = MockSearchMetadata(item1, TargetFrameworkMoniker.NetStandard10, TargetFrameworkMoniker.NetStandard20);
            var metadata2 = MockSearchMetadata(item2, TargetFrameworkMoniker.Net45, TargetFrameworkMoniker.Net50);
            var metadata3 = MockSearchMetadata(item3, TargetFrameworkMoniker.Net50_Linux, TargetFrameworkMoniker.Net50_Windows);

            var loader = GetDefaultPackageLoader();

            foreach (var metadata in Permute(metadata1, metadata2, metadata3))
            {
                // Act
                // Convert to enumerable since xunit doesn't have great support for IAsyncEnumerable yet
                var result = loader.FilterSearchResultsAsync(item1.Name, metadata, new[] { TargetFrameworkMoniker.Net50, TargetFrameworkMoniker.NetStandard20 }).ToEnumerable();

                // Assert
                Assert.Collection(result, r => Assert.Equal(r, item1));
            }
        }

        [Fact]
        public void Filter3Only1SupportedMultipleTfmsNoMatches()
        {
            // Arrange
            var item1 = _fixture.Create<NuGetReference>() with { Version = "1.0.0" };
            var item2 = item1 with { Version = "2.0.0" };
            var item3 = item1 with { Version = "3.0.0" };

            var metadata1 = MockSearchMetadata(item1, TargetFrameworkMoniker.Net45, TargetFrameworkMoniker.NetCoreApp21);
            var metadata2 = MockSearchMetadata(item2, TargetFrameworkMoniker.Net45, TargetFrameworkMoniker.Net50);
            var metadata3 = MockSearchMetadata(item3, TargetFrameworkMoniker.Net50_Linux, TargetFrameworkMoniker.Net50_Windows);

            var loader = GetDefaultPackageLoader();

            foreach (var metadata in Permute(metadata1, metadata2, metadata3))
            {
                // Act
                // Convert to enumerable since xunit doesn't have great support for IAsyncEnumerable yet
                var result = loader.FilterSearchResultsAsync(item1.Name, metadata, new[] { TargetFrameworkMoniker.Net50, TargetFrameworkMoniker.NetStandard20 }).ToEnumerable();

                // Assert
                Assert.Empty(result);
            }
        }

        [Fact]
        public void FilterOnly()
        {
            // Arrange
            var item1 = _fixture.Create<NuGetReference>() with { Version = "1.0.0" };
            var item2 = item1 with { Version = "1.1.0" };
            var item3 = item1 with { Version = "1.2.0" };
            var item4 = item1 with { Version = "2.1.0" };

            var metadata1 = MockSearchMetadata(item1, TargetFrameworkMoniker.NetStandard20);
            var metadata2 = MockSearchMetadata(item2, TargetFrameworkMoniker.NetStandard20);
            var metadata3 = MockSearchMetadata(item3, TargetFrameworkMoniker.NetStandard20);
            var metadata4 = MockSearchMetadata(item4, TargetFrameworkMoniker.NetStandard20);

            var loader = GetDefaultPackageLoader();

            foreach (var metadata in Permute(metadata1, metadata2, metadata3, metadata4))
            {
                // Act
                // Convert to enumerable since xunit doesn't have great support for IAsyncEnumerable yet
                var result = loader.FilterSearchResultsAsync(item1.Name, metadata, new[] { TargetFrameworkMoniker.Net50 }, latestMinorAndBuildOnly: true).ToEnumerable();

                // Assert
                Assert.Collection(result,
                    r => Assert.Equal(r, item3),
                    r => Assert.Equal(r, item4));
            }
        }

        private static IReadOnlyCollection<IReadOnlyCollection<T>> Permute<T>(params T[] nums)
        {
            var list = new List<IReadOnlyCollection<T>>();
            return DoPermute(nums, 0, nums.Length - 1, list);

            static List<IReadOnlyCollection<T>> DoPermute(T[] nums, int start, int end, List<IReadOnlyCollection<T>> list)
            {
                if (start == end)
                {
                    // We have one of our possible n! solutions,
                    // add it to the list.
                    list.Add(new List<T>(nums));
                }
                else
                {
                    for (var i = start; i <= end; i++)
                    {
                        Swap(ref nums[start], ref nums[i]);
                        DoPermute(nums, start + 1, end, list);
                        Swap(ref nums[start], ref nums[i]);
                    }
                }

                return list;

                static void Swap(ref T a, ref T b)
                {
                    var temp = a;
                    a = b;
                    b = temp;
                }
            }
        }

        // Create default package loader without any package sources
        private PackageLoader GetDefaultPackageLoader() =>
            new PackageLoader(
                new SourceCacheContext(),
                Enumerable.Empty<PackageSource>(),
                Options.Create<NuGetDownloaderOptions>(new NuGetDownloaderOptions()),
                new NullLogger<PackageLoader>());
    }
}

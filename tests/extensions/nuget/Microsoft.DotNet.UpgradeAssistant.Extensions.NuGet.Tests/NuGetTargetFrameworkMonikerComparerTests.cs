// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using static Microsoft.DotNet.UpgradeAssistant.TargetFrameworkMonikerParser;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet.Tests
{
    public class NuGetTargetFrameworkMonikerComparerTests
    {
        [InlineData(Net50, NetCoreApp31, true)]
        [InlineData(Net50, Net50, true)]
        [InlineData(Net50, NetStandard20, true)]
        [InlineData(Net50, NetStandard10, true)]
        [InlineData(Net50, NetStandard21, true)]
        [InlineData(Net50, Net48, false)]
        [InlineData(Net50, Net471, false)]
        [InlineData(Net50, Net47, false)]
        [InlineData(Net50, Net45, false)]
        [InlineData(NetCoreApp31, NetCoreApp31, true)]
        [InlineData(NetCoreApp31, Net50, false)]
        [InlineData(NetCoreApp31, NetStandard20, true)]
        [InlineData(NetCoreApp31, NetStandard10, true)]
        [InlineData(NetCoreApp31, NetStandard21, true)]
        [InlineData(NetCoreApp31, Net48, false)]
        [InlineData(NetCoreApp31, Net471, false)]
        [InlineData(NetCoreApp31, Net47, false)]
        [InlineData(NetCoreApp31, Net45, false)]
        [InlineData(Net48, NetCoreApp31, false)]
        [InlineData(Net471, NetCoreApp31, false)]
        [InlineData(Net471, Net50, false)]
        [InlineData(Net471, NetStandard20, false)]
        [InlineData(Net461, NetStandard20, false)]
        [Theory]
        public void IsCoreCompatibleSDKTargetFramework(string target, string tfm, bool isCompatible)
        {
            var tfmComparer = new NuGetTargetFrameworkMonikerComparer(new NullLogger<NuGetTargetFrameworkMonikerComparer>());
            var result = tfmComparer.IsCompatible(ParseTfm(target), ParseTfm(tfm));

            Assert.Equal(isCompatible, result);
        }

        [InlineData(null, null, 0)]
        [InlineData(null, NetCoreApp31, -1)]
        [InlineData(NetCoreApp31, null, 1)]
        [InlineData(Net50, NetCoreApp31, 1)]
        [InlineData(Net50, Net50, 0)]
        [InlineData(Net50, NetStandard20, 1)]
        [InlineData(Net50, NetStandard10, 1)]
        [InlineData(Net50, NetStandard21, 1)]
        [InlineData(Net50, Net48, -1)]
        [InlineData(Net50, Net471, -1)]
        [InlineData(Net50, Net47, -1)]
        [InlineData(Net50, Net45, -1)]
        [InlineData(NetCoreApp31, NetCoreApp31, 0)]
        [InlineData(NetCoreApp31, Net50, -1)]
        [InlineData(NetCoreApp31, NetStandard20, 1)]
        [InlineData(NetCoreApp31, NetStandard10, 1)]
        [InlineData(NetCoreApp31, NetStandard21, 1)]
        [InlineData(NetCoreApp31, Net48, -1)]
        [InlineData(NetCoreApp31, Net471, -1)]
        [InlineData(NetCoreApp31, Net47, -1)]
        [InlineData(NetCoreApp31, Net45, -1)]
        [InlineData(Net48, NetCoreApp31, -1)]
        [InlineData(Net471, NetCoreApp31, -1)]
        [InlineData(Net471, Net50, -1)]
        [InlineData(Net471, NetStandard20, -1)]
        [InlineData(Net461, NetStandard20, -1)]
        [InlineData(Net46, NetStandard20, -1)]
        [Theory]
        public void TfmCompare(string target, string tfm, int expected)
        {
            var tfmComparer = new NuGetTargetFrameworkMonikerComparer(new NullLogger<NuGetTargetFrameworkMonikerComparer>());
            var result = tfmComparer.Compare(Create(target), Create(tfm));

            Assert.Equal(expected, result);

            static TargetFrameworkMoniker? Create(string? input)
                => input is null ? null : ParseTfm(input);
        }

        [InlineData(Net60_Windows, Net50_Windows, Net60)]
        [InlineData(Net60_Windows, Net60, Net50_Windows)]
        [InlineData(Net60_Windows, Net50, Net60_Windows)]
        [InlineData(Net60_Windows_10_0_5, Net50, Net60_Windows_10_0_5)]
        [InlineData(Net60_Windows_10_0_5, Net50_Windows_10_0_5, Net60)]
        [InlineData(Net60_Windows_10_1_5, Net50_Windows_10_1_5, Net60_Windows_10_0_5)]
        [InlineData(Net60_Windows_10_1_5, Net50_Windows_10_1_5, Net60_Windows_10_1_5)]
        [InlineData(Net60_Windows_10_1_5, Net60_Windows_10_0_5, Net50_Windows_10_1_5)]
        [InlineData(Net60_Windows, Net60_Windows, Net50_Windows)]
        [InlineData(null, Net60_Linux, Net50_Windows)]
        [InlineData(Net60, Net50, Net60)]
        [InlineData(Net50, Net462, Net50)]
        [InlineData(Net50, Net50, NetStandard20)]
        [InlineData(NetStandard20, NetStandard20, NetStandard20)]
        [InlineData(NetStandard21, NetStandard20, NetStandard21)]
        [Theory]
        public void MergeTests(string expected, string tfm1, string tfm2)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var comparer = mock.Create<NuGetTargetFrameworkMonikerComparer>();

            // Act
            var didMerge = comparer.TryMerge(ParseTfm(tfm1), ParseTfm(tfm2), out var result);

            // Assert
            if (expected is not null)
            {
                Assert.True(didMerge);
                Assert.Equal(ParseTfm(expected), result);
            }
            else
            {
                Assert.False(didMerge);
                Assert.Null(result);
            }
        }
    }
}

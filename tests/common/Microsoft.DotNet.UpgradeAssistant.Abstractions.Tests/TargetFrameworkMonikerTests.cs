// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

using static Microsoft.DotNet.UpgradeAssistant.TargetFrameworkMonikerParser;

namespace Microsoft.DotNet.UpgradeAssistant.Abstractions.Tests
{
    public class TargetFrameworkMonikerTests
    {
        public static IEnumerable<object[]> EqualityData()
        {
            yield return new object[] { TargetFrameworkMoniker.Net45, TargetFrameworkMoniker.Net45 };
            yield return new object[] { TargetFrameworkMoniker.Net45, TargetFrameworkMoniker.Net45 with { Framework = "NET" } };
            yield return new object[] { TargetFrameworkMoniker.Net45, TargetFrameworkMoniker.Net45 with { FrameworkVersion = new Version(4, 5, 0) } };
        }

        [MemberData(nameof(EqualityData))]
        [Theory]
        public void EqualsTests(TargetFrameworkMoniker tfm1, TargetFrameworkMoniker tfm2)
        {
            Assert.Equal(tfm1, tfm2);
        }

        [InlineData(NetStandard10)]
        [InlineData(NetStandard20)]
        [InlineData(NetStandard21)]
        [InlineData(NetCoreApp21)]
        [InlineData(NetCoreApp30)]
        [InlineData(NetCoreApp31)]
        [InlineData(Net45)]
        [InlineData(Net46)]
        [InlineData(Net461)]
        [InlineData(Net462)]
        [InlineData(Net47)]
        [InlineData(Net471)]
        [InlineData(Net48)]
        [InlineData(Net50)]
        [InlineData(Net50_Windows)]
        [InlineData(Net50_Windows_10_0_5)]
        [InlineData(Net60)]
        [InlineData(Net60_Windows)]
        [InlineData(Net60_Windows_10_0_5)]
        [Theory]
        public void ToStringTests(string input)
        {
            // Arrange
            var tfm = ParseTfm(input);

            // Act
            var result = tfm.ToString();

            // Assert
            Assert.Equal(input, result);
        }

        public static IEnumerable<object[]> AlternateNames
        {
            get
            {
                yield return new object[] { new TargetFrameworkMoniker(".NETFramework", new Version(4, 5, 0)), Net45 };
                yield return new object[] { new TargetFrameworkMoniker(".NETCoreApp", new Version(5, 0)), Net50 };
                yield return new object[] { new TargetFrameworkMoniker("net", new Version(5, 0, 0)), Net50 };
                yield return new object[] { TargetFrameworkMoniker.Net50 with { Platform = string.Empty }, Net50 };
                yield return new object[] { TargetFrameworkMoniker.Net50_Windows with { PlatformVersion = new Version(0, 0, 0, 0) }, Net50_Windows };
                yield return new object[] { new TargetFrameworkMoniker(".NETCoreApp", new Version(5, 0, 0)), Net50 };
                yield return new object[] { new TargetFrameworkMoniker(".NETStandard", new Version(2, 1, 0)), NetStandard21 };
            }
        }

        [MemberData(nameof(AlternateNames))]
        [Theory]
        public void AlternateNamesTests(TargetFrameworkMoniker tfm, string expected)
        {
            Assert.Equal(expected, tfm.ToString());
        }

        [InlineData(NetStandard10, true)]
        [InlineData(NetStandard20, true)]
        [InlineData(NetStandard21, true)]
        [InlineData(NetCoreApp30, false)]
        [InlineData(NetCoreApp31, false)]
        [InlineData(Net45, false)]
        [InlineData(Net46, false)]
        [InlineData(Net461, false)]
        [InlineData(Net462, false)]
        [InlineData(Net47, false)]
        [InlineData(Net471, false)]
        [InlineData(Net48, false)]
        [InlineData(Net50, false)]
        [InlineData(Net50_Windows, false)]
        [InlineData(Net50_Windows_10_0_5, false)]
        [InlineData(Net60, false)]
        [InlineData(Net60_Windows, false)]
        [InlineData(Net60_Windows_10_0_5, false)]
        [Theory]
        public void IsStandardTests(string input, bool isStandard)
        {
            var tfm = ParseTfm(input);
            Assert.Equal(isStandard, tfm.IsNetStandard);
        }

        [InlineData(NetStandard10, false)]
        [InlineData(NetStandard20, false)]
        [InlineData(NetStandard21, false)]
        [InlineData(NetCoreApp30, false)]
        [InlineData(NetCoreApp31, false)]
        [InlineData(Net45, true)]
        [InlineData(Net46, true)]
        [InlineData(Net461, true)]
        [InlineData(Net462, true)]
        [InlineData(Net47, true)]
        [InlineData(Net471, true)]
        [InlineData(Net48, true)]
        [InlineData(Net50, false)]
        [InlineData(Net50_Windows, false)]
        [InlineData(Net50_Windows_10_0_5, false)]
        [InlineData(Net60, false)]
        [InlineData(Net60_Windows, false)]
        [InlineData(Net60_Windows_10_0_5, false)]
        [Theory]
        public void IsFrameworkTests(string input, bool isFramework)
        {
            var tfm = ParseTfm(input);
            Assert.Equal(isFramework, tfm.IsFramework);
        }

        [InlineData(NetStandard10, false)]
        [InlineData(NetStandard20, false)]
        [InlineData(NetStandard21, false)]
        [InlineData(NetCoreApp30, true)]
        [InlineData(NetCoreApp31, true)]
        [InlineData(Net45, false)]
        [InlineData(Net46, false)]
        [InlineData(Net461, false)]
        [InlineData(Net462, false)]
        [InlineData(Net47, false)]
        [InlineData(Net471, false)]
        [InlineData(Net48, false)]
        [InlineData(Net50, true)]
        [InlineData(Net50_Windows, true)]
        [InlineData(Net50_Windows_10_0_5, true)]
        [InlineData(Net60, true)]
        [InlineData(Net60_Windows, true)]
        [InlineData(Net60_Windows_10_0_5, true)]
        [Theory]
        public void IsNetCoreTests(string input, bool isNetCore)
        {
            var tfm = ParseTfm(input);
            Assert.Equal(isNetCore, tfm.IsNetCore);
        }
    }
}

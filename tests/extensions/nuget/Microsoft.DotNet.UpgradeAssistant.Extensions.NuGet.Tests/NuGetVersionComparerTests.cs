// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Autofac.Extras.Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet.Tests
{
    public class NuGetVersionComparerTests
    {
        [InlineData("1.0", "1.0", 0)]
        [InlineData("1.0", "2.0", -1)]
        [InlineData("1.0", "1.1", -1)]
        [InlineData("1.1", "1.0", 1)]
        [InlineData("2.0", "1.0", 1)]
        [Theory]
        public void StringCompareTests(string x, string y, int expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            // Act
            var result = mock.Create<NuGetVersionComparer>().Compare(x, y);

            // Assert
            Assert.Equal(expected, result);
        }

        [InlineData("1.0", "1.0", false)]
        [InlineData("1.0", "2.0", true)]
        [InlineData("1.0", "1.1", false)]
        [InlineData("1.1", "1.0", false)]
        [InlineData("2.0", "1.0", true)]
        [InlineData("2.*", "1.0", true)]
        [InlineData("2.0", "1.*", true)]
        [InlineData("1.0", "1.*", false)]
        [Theory]
        public void IsMajorVersionChange(string x, string y, bool expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            // Act
            var result = mock.Create<NuGetVersionComparer>().IsMajorChange(x, y);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}

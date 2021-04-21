﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild.Tests
{
    [Collection(MSBuildStepTestCollection.Name)]
    public class TargetFrameworkIdentifierTests
    {
        [InlineData("net5.0", "netcoreapp3.1", true)]
        [InlineData("net5.0", "net5.0", true)]
        [InlineData("net5.0", "netstandard2.0", true)]
        [InlineData("net5.0", "netstandard1.0", true)]
        [InlineData("net5.0", "netstandard2.1", true)]
        [InlineData("net5.0", "net48", false)]
        [InlineData("net5.0", "net471", false)]
        [InlineData("net5.0", "net47", false)]
        [InlineData("net5.0", "net45", false)]
        [InlineData("netcoreapp3.1", "netcoreapp3.1", true)]
        [InlineData("netcoreapp3.1", "net5.0", false)]
        [InlineData("netcoreapp3.1", "netstandard2.0", true)]
        [InlineData("netcoreapp3.1", "netstandard1.0", true)]
        [InlineData("netcoreapp3.1", "netstandard2.1", true)]
        [InlineData("netcoreapp3.1", "net48", false)]
        [InlineData("netcoreapp3.1", "net471", false)]
        [InlineData("netcoreapp3.1", "net47", false)]
        [InlineData("netcoreapp3.1", "net45", false)]
        [InlineData("net48", "netcoreapp3.1", false)]
        [InlineData("net471", "netcoreapp3.1", false)]
        [InlineData("net471", "net5.0", false)]
        [InlineData("net471", "netstandard2.0", false)]
        [InlineData("net461", "netstandard2.0", false)]
        [InlineData("net46", "netstandard2.0", false)]
        [Theory]
        public void IsCoreCompatibleSDKTargetFramework(string target, string tfm, bool isCompatible)
        {
            var tfmComparer = new NuGetTargetFrameworkMonikerComparer(new NullLogger<NuGetTargetFrameworkMonikerComparer>());
            var result = tfmComparer.IsCompatible(new TargetFrameworkMoniker(target), new TargetFrameworkMoniker(tfm));

            Assert.Equal(isCompatible, result);
        }

        [InlineData(null, null, 0)]
        [InlineData(null, "netcoreapp3.1", -1)]
        [InlineData("netcoreapp3.1", null, 1)]
        [InlineData("net5.0", "netcoreapp3.1", 1)]
        [InlineData("net5.0", "net5.0", 0)]
        [InlineData("net5.0", "netstandard2.0", 1)]
        [InlineData("net5.0", "netstandard1.0", 1)]
        [InlineData("net5.0", "netstandard2.1", 1)]
        [InlineData("net5.0", "net48", -1)]
        [InlineData("net5.0", "net471", -1)]
        [InlineData("net5.0", "net47", -1)]
        [InlineData("net5.0", "net45", -1)]
        [InlineData("netcoreapp3.1", "netcoreapp3.1", 0)]
        [InlineData("netcoreapp3.1", "net5.0", -1)]
        [InlineData("netcoreapp3.1", "netstandard2.0", 1)]
        [InlineData("netcoreapp3.1", "netstandard1.0", 1)]
        [InlineData("netcoreapp3.1", "netstandard2.1", 1)]
        [InlineData("netcoreapp3.1", "net48", -1)]
        [InlineData("netcoreapp3.1", "net471", -1)]
        [InlineData("netcoreapp3.1", "net47", -1)]
        [InlineData("netcoreapp3.1", "net45", -1)]
        [InlineData("net48", "netcoreapp3.1", -1)]
        [InlineData("net471", "netcoreapp3.1", -1)]
        [InlineData("net471", "net5.0", -1)]
        [InlineData("net471", "netstandard2.0", -1)]
        [InlineData("net461", "netstandard2.0", -1)]
        [InlineData("net46", "netstandard2.0", -1)]
        [Theory]
        public void TfmCompare(string target, string tfm, int expected)
        {
            var tfmComparer = new NuGetTargetFrameworkMonikerComparer(new NullLogger<NuGetTargetFrameworkMonikerComparer>());
            var result = tfmComparer.Compare(Create(target), Create(tfm));

            Assert.Equal(expected, result);

            static TargetFrameworkMoniker? Create(string? input)
                => input is null ? null : new TargetFrameworkMoniker(input);
        }

        [InlineData("net6.0-windows", "net5.0-windows", "net6.0")]
        [InlineData("net6.0-windows", "net6.0", "net5.0-windows")]
        [InlineData("net6.0-windows", "net5.0", "net6.0-windows")]
        [InlineData("net6.0-windows10.0.5", "net5.0", "net6.0-windows10.0.5")]
        [InlineData("net6.0-windows10.0.5", "net5.0-windows10.0.5", "net6.0")]
        [InlineData("net6.0-windows10.1.5", "net5.0-windows10.1.5", "net6.0-windows10.0.5")]
        [InlineData("net6.0-windows10.1.5", "net5.0-windows10.1.5", "net6.0-windows10.1.5")]
        [InlineData("net6.0-windows", "net6.0-windows", "net5.0-windows")]
        [InlineData(null, "net6.0-linux", "net5.0-windows")]
        [InlineData("net6.0", "net5.0", "net6.0")]
        [InlineData("net5.0", "net462", "net5.0")]
        [InlineData("net5.0", "net5.0", "netstandard2.0")]
        [InlineData("netstandard2.0", "netstandard2.0", "netstandard2.0")]
        [InlineData("netstandard2.1", "netstandard2.0", "netstandard2.1")]
        [Theory]
        public void MergeTests(string expected, string tfm1, string tfm2)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var comparer = mock.Create<NuGetTargetFrameworkMonikerComparer>();

            // Act
            var didMerge = comparer.TryMerge(new TargetFrameworkMoniker(tfm1), new TargetFrameworkMoniker(tfm2), out var result);

            // Assert
            if (expected is not null)
            {
                Assert.True(didMerge);
                Assert.Equal(new TargetFrameworkMoniker(expected), result);
            }
            else
            {
                Assert.False(didMerge);
                Assert.Null(result);
            }
        }
    }
}

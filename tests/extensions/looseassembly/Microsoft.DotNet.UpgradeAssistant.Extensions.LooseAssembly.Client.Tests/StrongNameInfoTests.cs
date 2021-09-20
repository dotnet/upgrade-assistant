// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Client.Tests
{
    public class StrongNameInfoTests
    {
        [Fact]
        public void ExtractInfoTest()
        {
            // Arrange
            var assembly = typeof(StrongNameInfoTests).Assembly;
            var assemblyName = assembly.GetName();

            // Act
            using var stream = File.OpenRead(assembly.Location);
            var strongNameInfo = StrongNameInfo.Get(stream)!;

            // Assert
            Assert.NotNull(strongNameInfo);
            Assert.Equal(assemblyName.Name, strongNameInfo.Name);
            Assert.True(strongNameInfo.PublicKeyToken.HasValue);
            Assert.Equal(assemblyName.GetPublicKeyToken(), strongNameInfo.PublicKeyToken!.Value.Bytes.ToArray());
        }

        [Fact]
        public void NotAValidPEFile()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            var strongNameInfo = StrongNameInfo.Get(stream)!;

            // Assert
            Assert.Null(strongNameInfo);
        }
    }
}

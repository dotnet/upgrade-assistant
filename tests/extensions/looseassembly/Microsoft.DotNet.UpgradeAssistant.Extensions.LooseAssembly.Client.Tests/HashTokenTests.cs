// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly.Client.Tests
{
    public class HashTokenTests
    {
        [Fact]
        public void VerifyHash()
        {
            // Arrange
            const string Expected = "F4-3A-33-7F-4A-B3-11-B5-23-80-FC-BE-F9-F2-60-8E";
            using var stream = typeof(HashTokenTests).Assembly.GetManifestResourceStream(typeof(HashTokenTests), "HashTokenTestFile.txt");

            // Act
            var token = HashToken.FromStream(stream!);

            // Assert
            var s = BitConverter.ToString(token.GetBytes().ToArray());
            Assert.Equal(Expected, s);
        }
    }
}

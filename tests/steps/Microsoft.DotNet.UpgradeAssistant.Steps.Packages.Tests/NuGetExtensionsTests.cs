using System;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Tests
{
    public class NuGetExtensionsTests
    {
        [Fact]
        public void GetNuGetVersionThrowsWhenNull()
        {
            NuGetReference? reference = default;

            Assert.Throws<ArgumentNullException>(() => reference!.GetNuGetVersion());
        }

        [InlineData("6.0.*")]
        [InlineData("4.*")]
        [InlineData("*")]
        [Theory]
        public void GetNuGetVersionCanHandleFloatingVersions(string version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            var indexOfWildCard = version.IndexOf("*", StringComparison.Ordinal);
            var expectedVersion = version.Substring(0, indexOfWildCard) + "0";
            if (version.Equals("*", StringComparison.Ordinal))
            {
                expectedVersion = "0.0";
            }

            var reference = new NuGetReference("Example", version);

            var actualVersion = reference.GetNuGetVersion();

            Assert.Equal(expectedVersion, actualVersion?.ToString());
        }
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.UpgradeAssistant.Steps.Packages.Test
{
    [TestClass]
    public class TargetFrameworkIdentifierTests
    {
        [DataTestMethod]
        [DataRow("net5.0", "netcoreapp3.1", true)]
        [DataRow("net5.0", "net5.0", true)]
        [DataRow("net5.0", "netstandard2.0", true)]
        [DataRow("net5.0", "netstandard1.0", true)]
        [DataRow("net5.0", "netstandard2.1", true)]
        [DataRow("net5.0", "net48", false)]
        [DataRow("net5.0", "net471", false)]
        [DataRow("net5.0", "net47", false)]
        [DataRow("net5.0", "net45", false)]
        [DataRow("netcoreapp3.1", "netcoreapp3.1", true)]
        [DataRow("netcoreapp3.1", "net5.0", false)]
        [DataRow("netcoreapp3.1", "netstandard2.0", true)]
        [DataRow("netcoreapp3.1", "netstandard1.0", true)]
        [DataRow("netcoreapp3.1", "netstandard2.1", true)]
        [DataRow("netcoreapp3.1", "net48", false)]
        [DataRow("netcoreapp3.1", "net471", false)]
        [DataRow("netcoreapp3.1", "net47", false)]
        [DataRow("netcoreapp3.1", "net45", false)]
        public void IsCoreCompatibleSDKTargetFramework(string target, string tfm, bool isCompatible)
        {
            var tfmComparer = new TargetFrameworkMonikerComparer(new NullLogger<TargetFrameworkMonikerComparer>());
            var result = tfmComparer.IsCompatible(new TargetFrameworkMoniker(target), new TargetFrameworkMoniker(tfm));

            Assert.AreEqual(isCompatible, result);
        }

        [DataTestMethod]
        [DataRow("net5.0", "netcoreapp3.1", true)]
        [DataRow("net5.0", "net5.0", true)]
        [DataRow("net5.0", "netstandard2.0", true)]
        [DataRow("net5.0", "netstandard1.0", true)]
        [DataRow("net5.0", "netstandard2.1", true)]
        [DataRow("net5.0", "net48", false)]
        [DataRow("net5.0", "net471", false)]
        [DataRow("net5.0", "net47", false)]
        [DataRow("net5.0", "net45", false)]
        [DataRow("netcoreapp3.1", "netcoreapp3.1", true)]
        [DataRow("netcoreapp3.1", "net5.0", false)]
        [DataRow("netcoreapp3.1", "netstandard2.0", true)]
        [DataRow("netcoreapp3.1", "netstandard1.0", true)]
        [DataRow("netcoreapp3.1", "netstandard2.1", true)]
        [DataRow("netcoreapp3.1", "net48", false)]
        [DataRow("netcoreapp3.1", "net471", false)]
        [DataRow("netcoreapp3.1", "net47", false)]
        [DataRow("netcoreapp3.1", "net45", false)]
        public void IsCoreCompatibleSDKTargetFrameworks(string target, string tfm, bool isCompatible)
        {
            var tfmComparer = new TargetFrameworkMonikerComparer(new NullLogger<TargetFrameworkMonikerComparer>());
            var result = tfmComparer.IsCompatible(new TargetFrameworkMoniker(target), new TargetFrameworkMoniker(tfm));

            Assert.AreEqual(isCompatible, result);
        }
    }
}

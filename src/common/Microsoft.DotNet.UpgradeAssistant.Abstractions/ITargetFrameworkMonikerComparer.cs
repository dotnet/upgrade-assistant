using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface ITargetFrameworkMonikerComparer : IComparer<TargetFrameworkMoniker>
    {
        /// <summary>
        /// Returns true if another tfm is compatible with a first tfm. For example, IsCompatible(net45, net40) should return true because
        /// net40-targeted dependencies are compatibile with net45.
        /// </summary>
        /// <param name="tfm">The TFM of the dependent project.</param>
        /// <param name="other">The TFM of the dependency.</param>
        /// <returns>True if the dependency is compatible with the dependent TFM, false otherwise.</returns>
        bool IsCompatible(TargetFrameworkMoniker tfm, TargetFrameworkMoniker other);
    }
}

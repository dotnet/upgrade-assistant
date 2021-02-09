using System;
using NuGet.Frameworks;

namespace Microsoft.UpgradeAssistant.MSBuild
{
    public class NuGetTargetFrameworkMonikerFactory : ITargetFrameworkMonikerFactory
    {
        public TargetFrameworkMoniker GetTFMForNetFxVersion(string netFxVersion)
        {
            if (netFxVersion is null)
            {
                throw new ArgumentNullException(nameof(netFxVersion));
            }

            var version = Version.Parse(netFxVersion.Trim('v', 'V'));
            var framework = new NuGetFramework(FrameworkConstants.FrameworkIdentifiers.Net, version);
            return new TargetFrameworkMoniker(framework.GetShortFolderName());
        }
    }
}

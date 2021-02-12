using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    internal static class NuGetExtensions
    {
        public static NuGetVersion? GetNuGetVersion(this NuGetReference nugetRef)
        {
            if (nugetRef.HasWildcardVersion)
            {
                return null;
            }

            return NuGetVersion.Parse(nugetRef.Version);
        }
    }
}

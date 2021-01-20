using NuGet.Versioning;

namespace AspNetMigrator.PackageUpdater
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

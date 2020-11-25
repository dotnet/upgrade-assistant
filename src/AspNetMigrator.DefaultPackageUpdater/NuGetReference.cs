using System;
using NuGet.Versioning;

namespace AspNetMigrator.Engine
{
    public record NuGetReference(string Name, string Version)
    {
        public bool HasWildcardVersion => Version.Equals("*", StringComparison.OrdinalIgnoreCase);

        public NuGetVersion? GetNuGetVersion()
        {
            if (HasWildcardVersion)
            {
                return null;
            }

            return NuGetVersion.Parse(Version);
        }

        public override string ToString()
        {
            return $"{Name}, Version={Version}";
        }
    }
}

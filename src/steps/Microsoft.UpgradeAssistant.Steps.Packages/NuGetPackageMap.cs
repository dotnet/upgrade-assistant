using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Versioning;

namespace AspNetMigrator.PackageUpdater
{
    public class NuGetPackageMap
    {
        public string PackageSetName { get; set; } = string.Empty;

        public IEnumerable<NuGetReference> NetFrameworkPackages { get; set; } = Enumerable.Empty<NuGetReference>();

        public IEnumerable<NuGetReference> NetCorePackages { get; set; } = Enumerable.Empty<NuGetReference>();

        /// <summary>
        /// Determines whether a package map's .NET Framework packages include a
        /// given package name and version.
        /// </summary>
        /// <param name="name">The package name to look for.</param>
        /// <param name="version">The package version to look for or null to match any version.</param>
        /// <returns>True if the package exists in NetFrameworkPackages with a version equal to or higher the version specified. Otherwise, false.</returns>
        public bool ContainsReference(string name, string? version)
        {
            // Check whether any NetFx packages have the right name
            var reference = NetFrameworkPackages.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            // If no packages matched, return false
            if (reference is null)
            {
                return false;
            }

            // If the version isn't specified, then matching the name is sufficient
            // Similarly, if the NetFx package has a wildcard version, then any version matches
            if (version is null || reference.HasWildcardVersion)
            {
                return true;
            }

            // Return false if the version is invalid
            if (!NuGetVersion.TryParse(version, out var parsedVersion))
            {
                return false;
            }

            // To match, the specified packged has to be the same version as the NetFx package or older
            var netFxVersion = reference.GetNuGetVersion();
            return parsedVersion <= netFxVersion;
        }
    }
}

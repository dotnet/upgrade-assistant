using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetMigrator.Engine
{
    public class NuGetPackageMap
    {
        public string PackageSetName { get; set; }

        public IEnumerable<NuGetReference> NetFrameworkPackages { get; set; }

        public IEnumerable<NuGetReference> NetCorePackages { get; set; }

        /// <summary>
        /// Determines whether a package map's .NET Framework packages include a
        /// given package name and version.
        /// </summary>
        /// <param name="name">The package name to look for.</param>
        /// <param name="version">The package version to look for or null to match any version.</param>
        /// <returns>True if the package exists in NetFrameworkPackages with a version equal to or higher the version specified. Otherwise, false.</returns>
        public bool ContainsReference(string name, string version)
        {
            // Check whether any NetFx packages have the right name
            var reference = NetFrameworkPackages.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            // If not packages matched, return false
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

            // To match, the specified packged has to be the same version as the NetFx package or older
            if (!Version.TryParse(version, out var parsedVersion))
            {
                // Return false if the version is invalid
                return false;
            }

            var netFxVersion = reference.GetVersion();

            if (parsedVersion.Major < netFxVersion.Major)
            {
                return true;
            }

            if (parsedVersion.Major > netFxVersion.Major)
            {
                return false;
            }

            if (parsedVersion.Minor < netFxVersion.Minor)
            {
                return true;
            }

            if (parsedVersion.Minor > netFxVersion.Minor)
            {
                return false;
            }

            if (parsedVersion.Build < netFxVersion.Build)
            {
                return true;
            }

            if (parsedVersion.Build > netFxVersion.Build)
            {
                return false;
            }

            if (parsedVersion.Revision < netFxVersion.Revision)
            {
                return true;
            }

            if (parsedVersion.Revision > netFxVersion.Revision)
            {
                return false;
            }

            // Return true if the version match exactly
            return true;
        }
    }
}

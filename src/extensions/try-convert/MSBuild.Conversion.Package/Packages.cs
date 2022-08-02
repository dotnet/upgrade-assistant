namespace MSBuild.Conversion.Package
{
    public class PackagesConfigPackage
    {
        /// <summary>
        /// Name of the package.
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// Exact version of the package depended upon.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Optional TFM that the package dependency applies to.
        /// </summary>
        public string? TargetFramework { get; set; }

        /// <summary>
        /// Optional string of allowed versions that follow the NuGet spec for syntax.
        /// </summary>
        public string? AllowedVersions { get; set; }

        /// <summary>
        /// Optional flag for use only in development; the package will not be included when a consuming package is created.
        /// </summary>
        public bool DevelopmentDependency { get; set; } = false;
    }

    public class PackageReferencePackage
    {
        /// <summary>
        /// Name of the package.
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// Exact version of the package depended upon.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Optional TFM that the package dependency applies to.
        /// </summary>
        public string? TargetFramework { get; set; }

        /// <summary>
        /// Optional flag for use only in development; the package will not be included when a consuming package is created.
        /// </summary>
        public bool DevelopmentDependency { get; set; } = false;

        public PackageReferencePackage(PackagesConfigPackage pcp)
        {
            ID = pcp.ID;
            Version = string.IsNullOrWhiteSpace(pcp.AllowedVersions) ? pcp.Version : pcp.AllowedVersions;
            TargetFramework = pcp.TargetFramework;
            DevelopmentDependency = pcp.DevelopmentDependency;
        }
    }
}

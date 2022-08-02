namespace MSBuild.Conversion.Facts
{
    /// <summary>
    /// A bunch of known values regarding NuGet.
    /// </summary>
    public static class PackageFacts
    {
        public const string PackagesConfigIncludeName = "packages.config";
        public const string PackageReferenceItemType = "PackageReference";
        public const string PackagesConfigIDName = "id";
        public const string PackagesConfigVersionName = "version";
        public const string PackagesConfigPackagesNodeName = "packages";
        public const string PackagesConfigTargetFrameworkName = "targetFramework";
        public const string PackagesConfigAllowedVersionsFrameworkname = "allowedVersions";
        public const string PackagesConfigDevelopmentDependencyName = "developmentDependency";
        public const string EnsureNuGetPackageBuildImportsName = "EnsureNuGetPackageBuildImports";
        public const string VersionAttribute = "Version";
    }
}

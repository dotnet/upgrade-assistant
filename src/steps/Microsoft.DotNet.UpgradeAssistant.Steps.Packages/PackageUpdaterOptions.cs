namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageUpdaterOptions
    {
        public string? MigrationAnalyzersPackageSource { get; set; }

        public string? MigrationAnalyzersPackageVersion { get; set; }

        public string? PackageMapPath { get; set; }
    }
}

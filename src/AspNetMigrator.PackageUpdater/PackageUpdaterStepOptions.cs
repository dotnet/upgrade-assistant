namespace AspNetMigrator.PackageUpdater
{
    public class PackageUpdaterStepOptions
    {
        public bool LogRestoreOutput { get; set; }

        public string? MigrationAnalyzersPackageSource { get; set; }

        public string? MigrationAnalyzersPackageVersion { get; set; }

        public string? PackageMapPath { get; set; }
    }
}

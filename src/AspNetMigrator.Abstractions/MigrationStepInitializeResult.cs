namespace AspNetMigrator
{
    public record MigrationStepInitializeResult(MigrationStepStatus Status, string Details, BuildBreakRisk Risk);
}

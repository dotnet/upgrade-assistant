namespace Microsoft.UpgradeAssistant
{
    public record MigrationStepInitializeResult(MigrationStepStatus Status, string Details, BuildBreakRisk Risk);
}

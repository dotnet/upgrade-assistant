namespace Microsoft.DotNet.UpgradeAssistant
{
    public record MigrationStepInitializeResult(MigrationStepStatus Status, string Details, BuildBreakRisk Risk);
}

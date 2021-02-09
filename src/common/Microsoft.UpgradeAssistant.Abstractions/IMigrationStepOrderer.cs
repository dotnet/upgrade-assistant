using System.Collections.Generic;

namespace Microsoft.UpgradeAssistant
{
    public interface IMigrationStepOrderer
    {
        bool TryAddStep(MigrationStep newStep);

        bool TryRemoveStep(string stepId);

        IEnumerable<MigrationStep> MigrationSteps { get; }
    }
}

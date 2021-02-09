using System.Collections.Generic;

namespace AspNetMigrator
{
    public interface IMigrationStepOrderer
    {
        bool TryAddStep(MigrationStep newStep);

        bool TryRemoveStep(string stepId);

        IEnumerable<MigrationStep> MigrationSteps { get; }
    }
}

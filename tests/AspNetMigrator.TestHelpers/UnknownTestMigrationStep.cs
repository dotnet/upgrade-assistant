using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.TestHelpers
{
    public class UnknownTestMigrationStep : TestMigrationStep
    {
        private const string UnknownMessage = "Test migration status unknown";

        public override string AppliedMessage => UnknownMessage;

        public override string InitializedMessage => UnknownMessage;

        public UnknownTestMigrationStep(string title, string? description = null, MigrationStep? parentStep = null, IEnumerable<MigrationStep>? subSteps = null, ILogger? logger = null)
            : base(title, description, parentStep, subSteps, logger)
        {
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Unknown, InitializedMessage, BuildBreakRisk.Unknown));

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepApplyResult(MigrationStepStatus.Unknown, AppliedMessage));
    }
}

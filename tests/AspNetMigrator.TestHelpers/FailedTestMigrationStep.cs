using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.TestHelpers
{
    public class FailedTestMigrationStep : TestMigrationStep
    {
        private const string FailedMessage = "Test migration step completed";

        public override string AppliedMessage => FailedMessage;

        public override string InitializedMessage => FailedMessage;

        public FailedTestMigrationStep(string title, string? description = null, MigrationStep? parentStep = null, IEnumerable<MigrationStep>? subSteps = null, MigrateOptions? options = null, ILogger? logger = null)
            : base(title, description, parentStep, subSteps, options, logger)
        {
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Failed, InitializedMessage, BuildBreakRisk.Unknown));

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepApplyResult(MigrationStepStatus.Failed, AppliedMessage));
    }
}

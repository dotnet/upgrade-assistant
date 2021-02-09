using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.UpgradeAssistant
{
    public class FailedTestMigrationStep : TestMigrationStep
    {
        public override string Id => typeof(FailedTestMigrationStep).FullName!;

        private const string FailedMessage = "Test migration step completed";

        public override string AppliedMessage => FailedMessage;

        public override string InitializedMessage => FailedMessage;

        public FailedTestMigrationStep(string title, string? description = null, MigrationStep? parentStep = null, IEnumerable<MigrationStep>? subSteps = null, ILogger? logger = null)
            : base(title, description, parentStep, subSteps, logger)
        {
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Failed, InitializedMessage, BuildBreakRisk.Unknown));

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepApplyResult(MigrationStepStatus.Failed, AppliedMessage));
    }
}

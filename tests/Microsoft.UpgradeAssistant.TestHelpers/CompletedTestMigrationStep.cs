using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UpgradeAssistant;

namespace AspNetMigrator.TestHelpers
{
    public class CompletedTestMigrationStep : TestMigrationStep
    {
        public override string Id => typeof(CompletedTestMigrationStep).FullName!;

        private const string CompletedMessage = "Test migration step completed";

        public override string AppliedMessage => CompletedMessage;

        public override string InitializedMessage => CompletedMessage;

        public CompletedTestMigrationStep(string title, string? description = null, MigrationStep? parentStep = null, IEnumerable<MigrationStep>? subSteps = null, ILogger? logger = null)
            : base(title, description, parentStep, subSteps, logger)
        {
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Complete, InitializedMessage, BuildBreakRisk.None));

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult(new MigrationStepApplyResult(MigrationStepStatus.Complete, AppliedMessage));
    }
}

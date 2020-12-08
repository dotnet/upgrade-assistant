using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.TestHelpers
{
    public class SkippedTestMigrationStep : TestMigrationStep
    {
        private const string SkippedMessage = "Test migration step skipped";

        public override string AppliedMessage => SkippedMessage;

        public override string InitializedMessage => SkippedMessage;

        public SkippedTestMigrationStep(string title, string? description = null, MigrationStep? parentStep = null, IEnumerable<MigrationStep>? subSteps = null, MigrateOptions? options = null, ILogger? logger = null)
            : base(title, description, parentStep, subSteps, options, logger)
        {
        }

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult((MigrationStepStatus.Skipped, InitializedMessage));

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult((MigrationStepStatus.Skipped, AppliedMessage));
    }
}

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

        public UnknownTestMigrationStep(string title, string? description = null, MigrationStep? parentStep = null, IEnumerable<MigrationStep>? subSteps = null, MigrateOptions? options = null, ILogger? logger = null)
            : base(title, description, parentStep, subSteps, options, logger)
        {
        }

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult((MigrationStepStatus.Unknown, InitializedMessage));

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult((MigrationStepStatus.Unknown, AppliedMessage));
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspNetMigrator.TestHelpers
{
    public class TestMigrationStep : MigrationStep
    {
        public int ApplicationCount { get; set; }

        public TestMigrationStep(
            string title,
            string? description = null,
            MigrationStep? parentStep = null,
            IEnumerable<MigrationStep>? subSteps = null,
            MigrateOptions? options = null,
            ILogger? logger = null)
            : base(options ?? Defaults.DefaultMigrateOptions, logger ?? new NullLogger<TestMigrationStep>())
        {
            Title = title;
            Description = description ?? string.Empty;
            ParentStep = parentStep;
            SubSteps = subSteps ?? Enumerable.Empty<MigrationStep>();
            ApplicationCount = 0;
        }

        public virtual string AppliedMessage => "Test migration step complete";

        public virtual string InitializedMessage => "Test migration step incomplete";

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            ApplicationCount++;
            return Task.FromResult((MigrationStepStatus.Complete, AppliedMessage));
        }

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync(IMigrationContext context, CancellationToken token) =>
            Task.FromResult((MigrationStepStatus.Incomplete, InitializedMessage));
    }
}

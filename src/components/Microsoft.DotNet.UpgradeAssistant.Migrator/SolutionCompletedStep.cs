using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Migrator
{
    internal class SolutionCompletedStep : MigrationStep
    {
        public SolutionCompletedStep(ILogger logger)
            : base(logger)
        {
        }

        public override string Id => typeof(SolutionCompletedStep).FullName!;

        public override string Title => "Complete Solution";

        public override string Description => "All projects have been upgraded. Please review any changes and test accordingly.";

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            context.IsComplete = true;
            context.SetEntryPoint(null);
            return Task.FromResult(new MigrationStepApplyResult(MigrationStepStatus.Complete, "Complete solution"));
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
            => Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, "Complete solution", BuildBreakRisk.None));

        protected override bool IsApplicableImpl(IMigrationContext context)
            => context.CurrentProject is null && context.EntryPoint is not null;
    }
}

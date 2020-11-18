using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public class Migrator
    {
        private readonly ImmutableArray<MigrationStep> _steps;

        private ILogger Logger { get; }

        public Migrator(IEnumerable<MigrationStep> steps, ILogger logger)
        {
            _steps = steps?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(steps));
            Logger = logger ?? new NullLogger();
        }

        public async Task<IEnumerable<MigrationStep>> GetInitializedStepsAsync(IMigrationContext context, CancellationToken token)
        {
            await InitializeNextStepAsync(_steps, context, token).ConfigureAwait(false);
            return _steps;
        }

        public async Task<MigrationStep> GetNextStepAsync(IMigrationContext context, CancellationToken token) =>
            GetCurrentStep(await GetInitializedStepsAsync(context, token).ConfigureAwait(false));

        private MigrationStep GetCurrentStep(IEnumerable<MigrationStep> steps)
        {
            if (steps is null)
            {
                return null;
            }

            foreach (var step in steps)
            {
                if (step.Status == MigrationStepStatus.Incomplete || step.Status == MigrationStepStatus.Failed)
                {
                    var nextSubStep = GetCurrentStep(step.SubSteps);
                    return nextSubStep ?? step;
                }
            }

            return null;
        }

        private async Task InitializeNextStepAsync(IEnumerable<MigrationStep> steps, IMigrationContext context, CancellationToken token)
        {
            if (steps is null)
            {
                return;
            }

            // Iterate steps, initializing any that are uninitialized until we come to an incomplete or failed step
            foreach (var step in steps)
            {
                if (step.Status == MigrationStepStatus.Unknown)
                {
                    // It is not necessary to iterate through sub-steps because parents steps are
                    // expected to initialize their children during their own initialization
                    Logger.Verbose("Initializing migration step {StepTitle}", step.Title);
                    await step.InitializeAsync(context, token).ConfigureAwait(false);
                    if (step.Status == MigrationStepStatus.Unknown)
                    {
                        Logger.Error("Migration step initialization failed for step {StepTitle}", step.Title);
                        throw new InvalidOperationException($"Step must not have unknown status after initialization. Step: {step.Title}");
                    }
                    else
                    {
                        Logger.Verbose("Step {StepTitle} initialized", step.Title);
                    }
                }

                if (step.Status == MigrationStepStatus.Incomplete || step.Status == MigrationStepStatus.Failed)
                {
                    // If the step is not complete, halt initialization
                    break;
                }
            }
        }
    }
}

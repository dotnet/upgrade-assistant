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
        private bool _initialized;

        public IEnumerable<MigrationStep> Steps => _initialized ? _steps : throw new InvalidOperationException("Migrator must be initialized prior top use");

        public MigrationStep NextStep => GetNextStep(Steps);

        private ILogger Logger { get; }

        public Migrator(IEnumerable<MigrationStep> steps, ILogger logger)
        {
            if (steps is null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            _steps = steps.ToImmutableArray();
            _initialized = false;
            Logger = logger ?? new NullLogger();
        }

        public async Task InitializeAsync(IMigrationContext context, CancellationToken token)
        {
            Logger.Verbose("Initializing migrator");

            await InitializeNextStepAsync(_steps, context, token).ConfigureAwait(false);

            _initialized = true;
            Logger.Verbose("Initialization complete");
        }

        private MigrationStep GetNextStep(IEnumerable<MigrationStep> steps)
        {
            if (steps is null)
            {
                return null;
            }

            foreach (var step in steps)
            {
                if (step.Status == MigrationStepStatus.Incomplete || step.Status == MigrationStepStatus.Failed)
                {
                    var nextSubStep = GetNextStep(step.SubSteps);
                    return nextSubStep ?? step;
                }
            }

            return null;
        }

        public async Task<bool> SkipNextStepAsync(IMigrationContext context, CancellationToken token)
        {
            Logger.Verbose("Skipping next migration step");

            var nextStep = NextStep;
            if (nextStep is null)
            {
                Logger.Information("No next migration step found");
                return false;
            }

            Logger.Information("Skipping migration step {StepTitle}", nextStep.Title);

            if (await nextStep.SkipAsync().ConfigureAwait(false))
            {
                Logger.Information("Migration step {StepTitle} skipped", nextStep.Title);
                await InitializeNextStepAsync(Steps, context, token).ConfigureAwait(false);
                return true;
            }
            else
            {
                Logger.Warning("Skipping migration step {StepTitle} failed: {Status}: {StatusDetail}", nextStep.Title, nextStep.Status, nextStep.StatusDetails);
                return false;
            }
        }

        public async Task<bool> ApplyNextStepAsync(IMigrationContext context, CancellationToken token)
        {
            Logger.Verbose("Applying next migration step");

            var nextStep = NextStep;
            if (nextStep is null)
            {
                Logger.Information("No next migration step found");
                return false;
            }

            Logger.Information("Applying migration step {StepTitle}", nextStep.Title);
            var success = await nextStep.ApplyAsync(context, token).ConfigureAwait(false);

            if (success)
            {
                Logger.Information("Migration step {StepTitle} applied successfully", nextStep.Title);
                await InitializeNextStepAsync(Steps, context, token).ConfigureAwait(false);
                return true;
            }
            else
            {
                Logger.Warning("Migration step {StepTitle} failed: {Status}: {StatusDetail}", nextStep.Title, nextStep.Status, nextStep.StatusDetails);
                return false;
            }
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

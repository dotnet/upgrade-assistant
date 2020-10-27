using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using AspNetMigrator.MSBuild;

namespace AspNetMigrator.Engine
{
    public class Migrator
    {
        private bool _initialized;
        private readonly ImmutableArray<MigrationStep> _steps;

        public IEnumerable<MigrationStep> Steps => _initialized ? _steps : throw new InvalidOperationException("Migrator must be initialized prior top use");
        private ILogger Logger { get; }


        public Migrator(MigrationStep[] steps, ILogger logger)
        {
            if (steps is null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            _steps = ImmutableArray.Create(steps);
            _initialized = false;
            Logger = logger ?? new NullLogger();
        }

        public async Task InitializeAsync()
        {
            Logger.Verbose("Initializing migrator");

            // Register correct MSBuild for use with SDK-style projects
            var msBuildPath = MSBuildHelper.RegisterMSBuildInstance();
            Logger.Verbose("MSBuild registered from {MSBuildPath}", msBuildPath);

            await InitializeNextStepAsync(_steps);

            _initialized = true;
            Logger.Verbose("Initialization complete");
        }

        public MigrationStep GetNextStep(IEnumerable<MigrationStep> steps)
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

        public async Task<bool> ApplyNextStepAsync()
        {
            Logger.Verbose("Applying next migration step");

            var nextStep = GetNextStep(Steps);
            if (nextStep is null)
            {
                Logger.Information("No next migration step found");
                return false;
            }

            Logger.Information("Applying migration step {StepTitle}", nextStep.Title);
            var success = await nextStep.ApplyAsync();

            if (success)
            {
                Logger.Information("Migration step {StepTitle} applied successfully", nextStep.Title);
                await InitializeNextStepAsync(Steps);
                return true;
            }
            else
            {
                Logger.Warning("Migration step {StepTitle} failed: {Status}: {StatusDetail}", nextStep.Title, nextStep.Status, nextStep.StatusDetails);
                return false;
            }
        }

        private async Task InitializeNextStepAsync(IEnumerable<MigrationStep> steps)
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
                    await step.InitializeAsync().ConfigureAwait(false);
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

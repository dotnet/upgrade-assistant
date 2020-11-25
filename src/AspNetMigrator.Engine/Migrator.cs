using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Engine
{
    public class Migrator
    {
        private readonly ImmutableArray<MigrationStep> _steps;

        private ILogger Logger { get; }

        public Migrator(IEnumerable<MigrationStep> steps, ILogger<Migrator> logger)
        {
            _steps = steps?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(steps));
            Logger = logger;
        }

        public IEnumerable<MigrationStep> Steps => _steps;

        public IAsyncEnumerable<MigrationStep> GetAllSteps(IMigrationContext context, CancellationToken token)
        {
            if (_steps.Length == 0)
            {
                Logger.LogError("No steps were registered for migration.");
                return AsyncEnumerable.Empty<MigrationStep>();
            }
            else
            {
                return GetStepsInternal(_steps, context, token);
            }
        }

        private async IAsyncEnumerable<MigrationStep> GetStepsInternal(IEnumerable<MigrationStep> steps, IMigrationContext context, [EnumeratorCancellation] CancellationToken token)
        {
            // This iterates through all incomplete steps, returning children before parents but initializing parents before children.
            // This is intentional because the expectation is that parents are initialized before children, but children are applied before parents.
            //
            // For each step, the expected order of operations is:
            // 1. Initialize the step
            // 2. Recurse into sub-steps (if needed)
            // 3. Yield the step if it's not completed, or
            //    continue iterating with the next step if it is.
            foreach (var step in steps)
            {
                if (step.Status == MigrationStepStatus.Unknown)
                {
                    // It is not necessary to iterate through sub-steps because parents steps are
                    // expected to initialize their children during their own initialization
                    Logger.LogInformation("Initializing migration step {StepTitle}", step.Title);
                    await step.InitializeAsync(context, token).ConfigureAwait(false);
                    if (step.Status == MigrationStepStatus.Unknown)
                    {
                        Logger.LogError("Migration step initialization failed for step {StepTitle}", step.Title);
                        throw new InvalidOperationException($"Step must not have unknown status after initialization. Step: {step.Title}");
                    }
                    else
                    {
                        Logger.LogDebug("Step {StepTitle} initialized", step.Title);
                    }
                }

                await foreach (var innerStep in GetStepsInternal(step.SubSteps, context, token))
                {
                    yield return innerStep;
                }

                if (!step.IsComplete)
                {
                    yield return step;
                }
            }
        }
    }
}

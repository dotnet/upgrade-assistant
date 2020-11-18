using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

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

        public IEnumerable<MigrationStep> Steps => _steps;

        public IAsyncEnumerable<MigrationStep> GetAllSteps(IMigrationContext context, CancellationToken token)
        {
            if (_steps.Length == 0)
            {
                Logger.Error("No steps were registered for migration.");
                return AsyncEnumerable.Empty<MigrationStep>();
            }
            else
            {
                return GetStepsInternal(_steps, context, token);
            }
        }

        private async IAsyncEnumerable<MigrationStep> GetStepsInternal(IEnumerable<MigrationStep> steps, IMigrationContext context, [EnumeratorCancellation] CancellationToken token)
        {
            foreach (var step in steps)
            {
                await foreach (var innerStep in GetStepsInternal(step.SubSteps, context, token))
                {
                    yield return innerStep;
                }

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

                if (!step.IsComplete)
                {
                    yield return step;
                }
            }
        }
    }
}

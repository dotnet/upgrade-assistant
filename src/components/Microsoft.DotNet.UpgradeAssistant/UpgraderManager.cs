// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class UpgraderManager
    {
        private readonly IUpgradeStepOrderer _orderer;
        private readonly ITelemetry _telemetry;
        private readonly IUserInput _userInput;
        private readonly ILogger _logger;

        public UpgraderManager(
            IUpgradeStepOrderer orderer,
            ITelemetry telemetry,
            IUserInput userInput,
            ILogger<UpgraderManager> logger)
        {
            _orderer = orderer ?? throw new ArgumentNullException(nameof(orderer));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<UpgradeStep> AllSteps => _orderer.UpgradeSteps;

        public async Task<IEnumerable<UpgradeStep>> InitializeAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _telemetry.TrackEvent("initialize", measurements: new Dictionary<string, double> { { "Project Count", context.Projects.Count() } });
            await _telemetry.TrackProjectPropertiesAsync(context, token).ConfigureAwait(false);

            context.CurrentStep = null;

            return AllSteps;
        }

        /// <summary>
        /// Finds and returns the next applicable and incomplete step for the given upgrade context. This will check whether each step
        /// is applicable (via IsApplicableAsync) and then iterate through the steps, intitializing those with a status of unknown,
        /// until one is found whose status is not complete or skipped. Then that step is returned.
        /// </summary>
        /// <param name="context">The upgrade context to evaluate upgrade steps against.</param>
        /// <returns>The next applicable but incomplete upgrade step, which should be the next upgrade step applied.
        /// Returns null if no upgrade steps need to be applied.</returns>
        public async Task<UpgradeStep?> GetNextStepAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            token.ThrowIfCancellationRequested();

            context.CurrentStep = null;

            // Get only steps that are applicable. This will check whether steps should be reset, reset them if necessary, and
            // check applicability.
            var steps = GetStepsForContextAsync(context, AllSteps);

            if (!await steps.AnyAsync(cancellationToken: token).ConfigureAwait(false))
            {
                _logger.LogDebug("No applicable upgrade steps found");
                return null;
            }

            if (await steps.AllAsync(s => s.IsDone, cancellationToken: token).ConfigureAwait(false))
            {
                _logger.LogDebug("All steps have completed");
                return null;
            }

            context.CurrentStep = await GetNextStepInternalAsync(steps, context, token).ConfigureAwait(false);

            if (context.CurrentStep is null)
            {
                context.CurrentStep = await GetNextStepAsync(context, token).ConfigureAwait(false);
            }

            if (context.CurrentStep is null)
            {
                _logger.LogDebug("No applicable incomplete upgrade steps found");
            }
            else
            {
                _logger.LogDebug("Identified upgrade step {UpgradeStep} as the next step", context.CurrentStep.Id);
            }

            return context.CurrentStep;
        }

        private async Task<UpgradeStep?> GetNextStepInternalAsync(IAsyncEnumerable<UpgradeStep> steps, IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // This iterates through all incomplete steps, returning children before parents but initializing parents before children.
            // This is intentional because the expectation is that parents are initialized before children, but children are applied before parents.
            //
            // For each step, the expected order of operations is:
            // 1. Initialize the step
            // 2. Recurse into sub-steps (if needed)
            // 3. Return the step if it's not completed, or
            //    continue iterating with the next step if it is.
            await foreach (var step in steps.ConfigureAwait(false))
            {
                context.CurrentStep = step;

                token.ThrowIfCancellationRequested();

                if (step.Status == UpgradeStepStatus.Unknown)
                {
                    // It is not necessary to iterate through sub-steps because parents steps are
                    // expected to initialize their children during their own initialization
                    _logger.LogInformation("Initializing upgrade step {StepTitle}", step.Title);

                    using (_telemetry.TimeStep("initialize", step))
                    {
                        await step.InitializeAsync(context, token).ConfigureAwait(false);
                    }

                    // This is actually not dead code. The above sentence InitializeAsync(...) call will potentially change the status.
#pragma warning disable CA1508 // Avoid dead conditional code
                    if (step.Status == UpgradeStepStatus.Unknown)
#pragma warning restore CA1508 // Avoid dead conditional code
                    {
                        _logger.LogError("Upgrade step initialization failed for step {StepTitle}", step.Title);
                        throw new InvalidOperationException($"Step must not have unknown status after initialization. Step: {step.Title}");
                    }
                    else
                    {
                        _logger.LogDebug("Step {StepTitle} initialized", step.Title);
                    }
                }

                // Recurse into substeps, return them first, and then return the parent step if it is still incomplete
                if (step.SubSteps.Any())
                {
                    var applicableSubSteps = GetStepsForContextAsync(context, step.SubSteps);
                    var nextSubStep = await GetNextStepInternalAsync(applicableSubSteps, context, token).ConfigureAwait(false);
                    if (nextSubStep is not null)
                    {
                        return nextSubStep;
                    }
                }

                if (step.Status == UpgradeStepStatus.Failed && !_userInput.IsInteractive)
                {
                    // Don't return failed steps in non-interactive mode
                    continue;
                }

                if (!step.IsDone)
                {
                    return step;
                }
            }

            return null;
        }

        private static IAsyncEnumerable<UpgradeStep> GetStepsForContextAsync(IUpgradeContext context, IEnumerable<UpgradeStep> steps)
            => steps.ToAsyncEnumerable().WhereAwaitWithCancellation(async (s, token) => await s.IsApplicableAsync(context, token).ConfigureAwait(false));
    }
}

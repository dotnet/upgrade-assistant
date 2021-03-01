﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class UpgraderManager
    {
        private readonly IEnumerable<IUpgradeReadyCheck> _checks;
        private readonly IPackageRestorer _restorer;
        private readonly IUpgradeStepOrderer _orderer;
        private readonly ILogger _logger;

        public UpgraderManager(
            IEnumerable<IUpgradeReadyCheck> checks,
            IPackageRestorer restorer,
            IUpgradeStepOrderer orderer,
            ILogger<UpgraderManager> logger)
        {
            _checks = checks ?? throw new ArgumentNullException(nameof(checks));
            _restorer = restorer ?? throw new ArgumentNullException(nameof(restorer));
            _orderer = orderer ?? throw new ArgumentNullException(nameof(orderer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<UpgradeStep> AllSteps => _orderer.UpgradeSteps;

        public async Task<IEnumerable<UpgradeStep>> InitializeAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.EntryPoint is not null)
            {
                await _restorer.RestorePackagesAsync(context, context.EntryPoint, token).ConfigureAwait(false);
            }

            if (!await RunChecksAsync(context, token).ConfigureAwait(false))
            {
                throw new UpgradeException("Readiness checks did not succeed. Please address any issues and try again.");
            }

            return GetStepsForContext(context);
        }

        private async Task<bool> RunChecksAsync(IUpgradeContext context, CancellationToken token)
        {
            var success = true;

            foreach (var check in _checks)
            {
                _logger.LogTrace("Running readiness check {Id}", check.Id);

                success &= await check.IsReadyAsync(context, token).ConfigureAwait(false);
            }

            return success;
        }

        public IEnumerable<UpgradeStep> GetStepsForContext(IUpgradeContext context) => AllSteps.Where(s => s.IsApplicable(context));

        /// <summary>
        /// Finds and returns the next applicable and incomplete step for the given upgrade context.
        /// </summary>
        /// <param name="context">The upgrade context to evaluate upgrade steps against.</param>
        /// <returns>The next applicable but incomplete upgrade step, which should be the next upgrade step applied.
        /// Returns null if no upgrade steps need to be applied.</returns>
        public async Task<UpgradeStep?> GetNextStepAsync(IUpgradeContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var steps = GetStepsForContext(context).ToList();

            if (!steps.Any())
            {
                _logger.LogDebug("No applicable upgrade steps found");
                return null;
            }

            if (steps.All(s => s.IsDone))
            {
                _logger.LogDebug("All steps have completed");
                return null;
            }

            var nextStep = await GetNextStepAsyncInternal(steps, context, token).ConfigureAwait(false);

            if (nextStep is null)
            {
                nextStep = await GetNextStepAsync(context, token).ConfigureAwait(false);
            }

            if (nextStep is null)
            {
                _logger.LogDebug("No applicable incomplete upgrade steps found");
            }
            else
            {
                _logger.LogDebug("Identified upgrade step {UpgradeStep} as the next step", nextStep.Id);
            }

            return nextStep;
        }

        private async Task<UpgradeStep?> GetNextStepAsyncInternal(IEnumerable<UpgradeStep> steps, IUpgradeContext context, CancellationToken token)
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
            foreach (var step in steps)
            {
                token.ThrowIfCancellationRequested();

                if (step.Status == UpgradeStepStatus.Unknown)
                {
                    // It is not necessary to iterate through sub-steps because parents steps are
                    // expected to initialize their children during their own initialization
                    _logger.LogInformation("Initializing upgrade step {StepTitle}", step.Title);
                    await step.InitializeAsync(context, token).ConfigureAwait(false);

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

                if (step.SubSteps.Any())
                {
                    var nextSubStep = await GetNextStepAsyncInternal(step.SubSteps, context, token).ConfigureAwait(false);
                    if (nextSubStep is not null)
                    {
                        return nextSubStep;
                    }
                }

                if (!step.IsDone)
                {
                    return step;
                }
            }

            return null;
        }
    }
}

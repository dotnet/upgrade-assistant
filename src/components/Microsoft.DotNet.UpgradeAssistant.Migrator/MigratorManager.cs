﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Migrator
{
    public class MigratorManager
    {
        private readonly IPackageRestorer _restorer;
        private readonly IMigrationStepOrderer _orderer;

        private ILogger Logger { get; }

        public MigratorManager(IPackageRestorer restorer, IMigrationStepOrderer orderer, ILogger<MigratorManager> logger)
        {
            _restorer = restorer ?? throw new ArgumentNullException(nameof(restorer));
            _orderer = orderer ?? throw new ArgumentNullException(nameof(orderer));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<MigrationStep> AllSteps => _orderer.MigrationSteps;

        public async Task<IEnumerable<MigrationStep>> InitializeAsync(IMigrationContext context, CancellationToken token)
        {
            if (context.EntryPoint is not null)
            {
                await _restorer.RestorePackagesAsync(context, context.EntryPoint, token);
            }

            return GetStepsForContext(context);
        }

        public IEnumerable<MigrationStep> GetStepsForContext(IMigrationContext context) => AllSteps.Where(s => s.IsApplicable(context));

        /// <summary>
        /// Finds and returns the next applicable and incomplete step for the given migration context.
        /// </summary>
        /// <param name="context">The migration context to evaluate migration steps against.</param>
        /// <returns>The next applicable but incomplete migration step, which should be the next migration step applied.
        /// Returns null if no migration steps need to be applied.</returns>
        public async Task<MigrationStep?> GetNextStepAsync(IMigrationContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var steps = GetStepsForContext(context).ToList();

            if (!steps.Any())
            {
                Logger.LogDebug("No applicable migration steps found");
                return null;
            }

            if (steps.All(s => s.IsDone))
            {
                Logger.LogDebug("All steps have completed");
                return null;
            }

            var nextStep = await GetNextStepAsyncInternal(steps, context, token).ConfigureAwait(false);

            if (nextStep is null)
            {
                nextStep = await GetNextStepAsync(context, token).ConfigureAwait(false);
            }

            if (nextStep is null)
            {
                Logger.LogDebug("No applicable incomplete migration steps found");
            }
            else
            {
                Logger.LogDebug("Identified migration step {MigrationStep} as the next step", nextStep.Id);
            }

            return nextStep;
        }

        private async Task<MigrationStep?> GetNextStepAsyncInternal(IEnumerable<MigrationStep> steps, IMigrationContext context, CancellationToken token)
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    /// <summary>
    /// Upgrade step that updates Winforms Projects.
    /// </summary>
    public class WinformsUpdateStep : UpgradeStep
    {
        public override string Title => "Update Winforms Project";

        public override string Description => "Update Winforms Project";

        public override string Id => WellKnownStepIds.WinformsProjectUpdaterStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing source
            WellKnownStepIds.BackupStepId,

            // Template files should be added prior to changing source (since some code fixers will change added templates)
            WellKnownStepIds.TemplateInserterStepId,

            // Project should have correct TFM
            WellKnownStepIds.SetTFMStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        public WinformsUpdateStep(IEnumerable<IUpdater<IProject>> winformsUpdaters, ILogger<WinformsUpdateStep> logger)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            SubSteps = new List<UpgradeStep>(winformsUpdaters.Select(updater => new WinformsUpdaterSubStep(this, updater, logger)));
        }

        /// <summary>
        /// Determines whether the WinformsUpdaterStep applies to a given context.
        /// </summary>
        /// <param name="context">The context to evaluate.</param>
        /// <param name="token">A token that can be used to cancel execution.</param>
        /// <returns>True if the Winforms updater step might apply, false otherwise.</returns>
        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context?.CurrentProject is null)
            {
                return false;
            }

            foreach (var subStep in SubSteps)
            {
                if (await subStep.IsApplicableAsync(context, token).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (var step in SubSteps)
            {
                await step.InitializeAsync(context, token).ConfigureAwait(false);
            }

            var incompleteSubSteps = SubSteps.Count(s => !s.IsDone);

            return incompleteSubSteps == 0
                ? new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No Winforms updaters need applied", BuildBreakRisk.None)
                : new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{incompleteSubSteps} Winforms updaters need applied", SubSteps.Where(s => !s.IsDone).Max(s => s.Risk));
        }

        public override UpgradeStepInitializeResult Reset()
        {
            return base.Reset();
        }

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var incompleteSubSteps = SubSteps.Count(s => !s.IsDone);

            return incompleteSubSteps == 0
                ? Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, string.Empty))
                : Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Incomplete, $"{incompleteSubSteps} Winforms updaters need applied"));
        }
    }
}

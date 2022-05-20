// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    /// <summary>
    /// Upgrade step that updates Winforms Projects.
    /// </summary>
    public class WindowsDesktopUpdateStep : UpgradeStep
    {
        public override string Title => "Update Windows Desktop Project";

        public override string Description => "Update Windows Desktop Project";

        public override string Id => WellKnownStepIds.WindowsDesktopUpdateStepId;

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

        public WindowsDesktopUpdateStep(IEnumerable<IUpdater<IProject>> winformsUpdaters, ILogger<WindowsDesktopUpdateStep> logger)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            SubSteps = winformsUpdaters.Select(updater => new WindowsDesktopUpdaterSubStep(this, updater, logger)).ToList();
        }

        /// <summary>
        /// Determines whether the WindowsDesktopUpdateStep applies to a given context.
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

        private async Task<IEnumerable<UpgradeStep>> GetApplicableSubSteps(IUpgradeContext context, CancellationToken token)
        {
            if (context?.CurrentProject is null)
            {
                return ImmutableList.Create<UpgradeStep>();
            }

            var applicableSteps = new List<UpgradeStep>();
            foreach (var subStep in SubSteps)
            {
                if (await subStep.IsApplicableAsync(context, token).ConfigureAwait(false))
                {
                    applicableSteps.Add(subStep);
                }
            }

            return applicableSteps;
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var applicableSubsteps = await GetApplicableSubSteps(context, token).ConfigureAwait(false);
            foreach (var step in applicableSubsteps)
            {
                await step.InitializeAsync(context, token).ConfigureAwait(false);
            }

            var incompleteSubSteps = applicableSubsteps.Count(s => !s.IsDone);

            return incompleteSubSteps == 0
                ? new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No Windows Desktop Updaters need applied", BuildBreakRisk.None)
                : new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{incompleteSubSteps} Windows Desktop updaters need applied", SubSteps.Where(s => !s.IsDone).Max(s => s.Risk));
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var applicableSubsteps = await GetApplicableSubSteps(context, token).ConfigureAwait(false);
            var incompleteSubSteps = applicableSubsteps.Count(s => !s.IsDone);

            return incompleteSubSteps == 0
                ? new UpgradeStepApplyResult(UpgradeStepStatus.Complete, string.Empty)
                : new UpgradeStepApplyResult(UpgradeStepStatus.Incomplete, $"{incompleteSubSteps} Windows Desktop updaters need applied");
        }
    }
}

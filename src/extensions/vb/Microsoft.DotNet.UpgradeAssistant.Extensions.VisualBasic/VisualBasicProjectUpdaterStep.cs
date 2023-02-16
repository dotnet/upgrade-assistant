// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic
{
    /// <summary>
    /// Upgrade step that applies changes specific to helping customers
    /// working on VB projects. Includes things that help to fix the "My."
    /// namespace and other things specific to VB.
    /// </summary>
    public class VisualBasicProjectUpdaterStep : UpgradeStep
    {
        public override string Title => "Update Visual Basic project";

        public override string Description => "Update Visual Basic projects using registered VB updaters";

        public override string Id => WellKnownStepIds.VisualBasicProjectUpdaterStepId;

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

        public VisualBasicProjectUpdaterStep(ILogger<VisualBasicProjectUpdaterStep> logger)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            // Add a sub-step for each VB specific fix
            SubSteps = new List<UpgradeStep>() { new EnableMyDotSupportSubStep(this, logger) };
        }

        /// <summary>
        /// Determines whether the VisualBasicProjectUpdaterStep applies to a given context.
        /// </summary>
        /// <param name="context">The context to evaluate.</param>
        /// <param name="token">A token that can be used to cancel execution.</param>
        /// <returns>True if there are applicable substeps, false otherwise.</returns>
        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            // The VisualBasicProjectUpdaterStep is only applicable when a project is loaded
            if (context?.CurrentProject is null || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            if (!(context?.CurrentProject.Language is Language.VisualBasic))
            {
                // this step only applies to VB projects
                return false;
            }

            // The VisualBasicProjectUpdaterStep is applicable if it contains at least one applicable substep
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
                ? new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No VB updaters need applied", BuildBreakRisk.None)
                : new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{incompleteSubSteps} VB updaters need applied", SubSteps.Where(s => !s.IsDone).Max(s => s.Risk));
        }

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            // Updates are made in sub-steps, so no changes need made in this apply step.
            // Just check that sub-steps executed correctly.
            var incompleteSubSteps = SubSteps.Count(s => !s.IsDone);

            return incompleteSubSteps == 0
                ? Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, string.Empty))
                : Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Incomplete, $"{incompleteSubSteps} VB updaters need applied"));
        }
    }
}

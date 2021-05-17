// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    internal class FinalizeSolutionStep : UpgradeStep
    {
        public FinalizeSolutionStep(ILogger<FinalizeSolutionStep> logger)
            : base(logger)
        {
        }

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        public override string Title => "Finalize upgrade";

        public override string Description => "All projects have been upgraded as much as the tool is capable at the moment. Please review any changes and test accordingly. By finalizing the solution, any state tracked by Upgrade Assistant will be removed and future sessions will recalculate any potential progress that has been made.";

        public override string Id => WellKnownStepIds.FinalizeSolutionStepId;

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            // Finalizing will remove any progress tracked by Upgrade Assistant.
            context.IsComplete = true;
            context.EntryPoints = Enumerable.Empty<IProject>();

            return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Upgrade complete"));
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context.EntryPoints.Any())
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "Finalize upgrade and delete .upgrade-assistant file", BuildBreakRisk.None));
            }
            else
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Upgrade completed", BuildBreakRisk.None));
            }
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
            => Task.FromResult(context.CurrentProject is null && context.EntryPoints.Any());
    }
}

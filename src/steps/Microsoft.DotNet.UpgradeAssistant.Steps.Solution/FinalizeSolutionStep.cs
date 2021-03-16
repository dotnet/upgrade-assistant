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

        public override string Description => "All projects have been upgraded. Please review any changes and test accordingly.";

        public override string Id => WellKnownStepIds.FinalizeSolutionStepId;

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            context.IsComplete = true;
            context.SetEntryPoint(null);
            return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Upgrade complete"));
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context.EntryPoint is null)
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Upgrade completed", BuildBreakRisk.None));
            }
            else
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"Finalize upgrade of entry point {context.EntryPoint.FilePath}", BuildBreakRisk.None));
            }
        }

        protected override bool IsApplicableImpl(IUpgradeContext context)
            => context.CurrentProject is null && context.EntryPoint is not null;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
            "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.NextProjectStep",
        };

        public override string Title => "Finalize Solution";

        public override string Description => "All projects have been upgraded. Please review any changes and test accordingly.";

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            context.IsComplete = true;
            context.SetEntryPoint(null);
            return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Complete solution"));
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context.IsComplete)
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Solution completed", BuildBreakRisk.None));
            }
            else
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "Complete solution", BuildBreakRisk.None));
            }
        }

        protected override bool IsApplicableImpl(IUpgradeContext context)
            => context.CurrentProject is null && context.EntryPoint is not null;
    }
}

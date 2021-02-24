// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    internal class NextProjectStep : UpgradeStep
    {
        public NextProjectStep(ILogger<NextProjectStep> logger)
            : base(logger)
        {
        }

        public override string Id => typeof(NextProjectStep).FullName!;

        public override string Title => "Move to next project";

        public override string Description => "The current project has completed upgrade. Please review any changes and ensure project is able to build before moving on.";

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            context.SetCurrentProject(null);
            return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Move to next project"));
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context.CurrentProject is null)
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No project selected", BuildBreakRisk.None));
            }
            else
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "Move to next project", BuildBreakRisk.None));
            }
        }

        protected override bool IsApplicableImpl(IUpgradeContext context)
            => context.CurrentProject is not null;
    }
}

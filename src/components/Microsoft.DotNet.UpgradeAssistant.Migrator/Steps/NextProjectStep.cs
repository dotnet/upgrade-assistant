// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Migrator.Steps
{
    internal class NextProjectStep : MigrationStep
    {
        public NextProjectStep(ILogger<NextProjectStep> logger)
            : base(logger)
        {
        }

        public override string Id => typeof(NextProjectStep).FullName!;

        public override string Title => "Move to next project";

        public override string Description => "The current project has completed upgrade. Please review any changes and ensure project is able to build before moving on.";

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            context.SetCurrentProject(null);
            return Task.FromResult(new MigrationStepApplyResult(MigrationStepStatus.Complete, "Move to next project"));
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context.CurrentProject is null)
            {
                return Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Complete, "No project selected", BuildBreakRisk.None));
            }
            else
            {
                return Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, "Move to next project", BuildBreakRisk.None));
            }
        }

        protected override bool IsApplicableImpl(IMigrationContext context)
            => context.CurrentProject is not null;
    }
}

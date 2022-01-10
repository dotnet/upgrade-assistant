// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public abstract class AutoApplySubStep : UpgradeStep
    {
        private readonly UpgradeStep _parentStep;

        protected AutoApplySubStep(UpgradeStep parentStep, ILogger logger)
            : base(logger)
        {
            _parentStep = parentStep;
        }

        public override async Task InitializeAsync(IUpgradeContext context, CancellationToken token)
        {
            await base.InitializeAsync(context, token).ConfigureAwait(false);

            // Normally, the upgrader will apply steps one at a time
            // at the user's instruction. In the case of parent and child steps,
            // the parent has any top-level application done after the children.
            // In the case of the ConfigUpdateStep, the parent (this step's parent)
            // doesn't need to apply anything.
            // Therefore, automatically apply the parent ConfigUpdaterStep's updater
            // once all its children have been applied.
            if (_parentStep.SubSteps.All(s => s.IsDone))
            {
                await _parentStep.ApplyAsync(context, token).ConfigureAwait(false);
            }
        }

        public override async Task<bool> ApplyAsync(IUpgradeContext context, CancellationToken token)
        {
            var result = await base.ApplyAsync(context, token).ConfigureAwait(false);

            // Normally, the upgrader will apply steps one at a time
            // at the user's instruction. In the case of parent and child steps,
            // the parent has any top-level application done after the children.
            // In the case of the ConfigUpdateStep, the parent (this step's parent)
            // doesn't need to apply anything.
            // Therefore, automatically apply the parent ConfigUpdaterStep's updater
            // once all its children have been applied.
            if (_parentStep.SubSteps.All(s => s.IsDone))
            {
                await _parentStep.ApplyAsync(context, token).ConfigureAwait(false);
            }

            return result;
        }
    }
}

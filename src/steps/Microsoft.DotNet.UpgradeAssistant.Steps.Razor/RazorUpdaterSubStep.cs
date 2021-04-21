// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    internal class RazorUpdaterSubStep : UpgradeStep
    {
        private RazorUpdaterStep _razorUpdaterStep;
        private IUpdater<RazorCodeDocument> _updater;

        public override string Id => _updater.Id;

        public override string Title => _updater.Title;

        public override string Description => _updater.Description;

        public RazorUpdaterSubStep(RazorUpdaterStep razorUpdaterStep, IUpdater<RazorCodeDocument> updater, ILogger<RazorUpdaterStep> logger)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            ParentStep = _razorUpdaterStep = razorUpdaterStep ?? throw new ArgumentNullException(nameof(razorUpdaterStep));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));
        }

        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            // Razor updates don't apply until a project is selected
            if (context?.CurrentProject is null)
            {
                return false;
            }

            // Check the updater for an [ApplicableComponents] attribute
            // If one exists, the step only applies if the project has the indicated components
            return await _updater.GetType().AppliesToProjectAsync(context.CurrentProject, token).ConfigureAwait(false);
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var updaterResult = (FileUpdaterResult)(await _updater.IsApplicableAsync(context, _razorUpdaterStep.RazorDocuments, token).ConfigureAwait(false));
                return updaterResult.Result
                    ? new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"Razor updater \"{_updater.Title}\" needs applied to {string.Join(", ", updaterResult.FilePaths)}", _updater.Risk)
                    : new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, string.Empty, BuildBreakRisk.None);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogError(exc, "Unexpected exception while initializing Razor updater \"{RazorUpdater}\"", _updater.Title);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Unexpected exception while initializing Razor updater \"{_updater.Title}\": {exc}", Risk);
            }
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var updaterResult = (FileUpdaterResult)(await _updater.ApplyAsync(context, _razorUpdaterStep.RazorDocuments, token).ConfigureAwait(false));
                if (updaterResult.Result)
                {
                    // Process Razor documents again after successfully applying an updater in case Razor files have changed
                    _razorUpdaterStep.ProcessRazorDocuments(updaterResult.FilePaths);

                    return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, string.Empty);
                }
                else
                {
                    return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Failed to apply Razor updater \"{_updater.Title}\"");
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogError(exc, "Unexpected exception while applying Razor updater \"{RazorUpdater}\"", _updater.Title);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Unexpected exception while applying Razor updater \"{_updater.Title}\": {exc}");
            }
        }

        /// <summary>
        /// Apply upgrade and update Status as necessary.
        /// </summary>
        /// <returns>True if the upgrade step was successfully applied or false if upgrade failed.</returns>
        public override async Task<bool> ApplyAsync(IUpgradeContext context, CancellationToken token)
        {
            var result = await base.ApplyAsync(context, token).ConfigureAwait(false);

            // Normally, the upgrader will apply steps one at a time
            // at the user's instruction. In the case of parent and child steps,
            // the parent has any top-level application done after the children.
            // In the case of the RazorUpdaterStep, the parent (this step's parent)
            // doesn't need to apply anything.
            // Therefore, automatically apply the parent RazerUpdaterStep's updater
            // once all its children have been applied.
            if (_razorUpdaterStep.SubSteps.All(s => s.IsDone))
            {
                await _razorUpdaterStep.ApplyAsync(context, token).ConfigureAwait(false);
            }

            return result;
        }
    }
}

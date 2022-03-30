﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    internal class WindowsDesktopUpdaterSubStep : UpgradeStep
    {
        private readonly WindowsDesktopUpdateStep _windowsDesktopUpdateStep;
        private readonly IUpdater<IProject> _updater;

        public override string Id => _updater.Id;

        public override string Title => _updater.Title;

        public override string Description => _updater.Description;

        public WindowsDesktopUpdaterSubStep(WindowsDesktopUpdateStep windowsDesktopUpdateStep, IUpdater<IProject> updater, ILogger<WindowsDesktopUpdateStep> logger)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            ParentStep = _windowsDesktopUpdateStep = windowsDesktopUpdateStep ?? throw new ArgumentNullException(nameof(windowsDesktopUpdateStep));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));
        }

        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context?.CurrentProject is null)
            {
                return false;
            }

            // Check the updater for an [ApplicableComponents] attribute
            // If one exists, the step only applies if the project has the indicated components
            return await context.CurrentProject.IsApplicableAsync(_updater, token).ConfigureAwait(false);
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var currentProject = context.CurrentProject.Required();

            try
            {
                var updaterResult = (WindowsDesktopUpdaterResult)(await _updater.IsApplicableAsync(context, ImmutableArray<IProject>.Empty.Add(currentProject), token).ConfigureAwait(false));
                return updaterResult.Result
                    ? new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"Windows Desktop updater \"{_updater.Title}\" needs to be applied", _updater.Risk)
                    : new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, string.Empty, BuildBreakRisk.None);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogError(exc, "Unexpected exception while initializing Windows Desktop updater \"{WinformsUpdater}\"", _updater.Title);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Unexpected exception while initializing Windows Desktop updater \"{_updater.Title}\": {exc}", Risk);
            }
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var currentProject = context.CurrentProject.Required();

            try
            {
                var updaterResult = (WindowsDesktopUpdaterResult)(await _updater.ApplyAsync(context, ImmutableArray<IProject>.Empty.Add(currentProject), token).ConfigureAwait(false));
                if (updaterResult.Result)
                {
                    return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, string.Empty);
                }
                else
                {
                    return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Failed to apply Windows Desktop updater \"{_updater.Title}\"");
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogError(exc, "Unexpected exception while applying Windows Desktop updater \"{WinformsUpdater}\"", _updater.Title);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Unexpected exception while applying Windows Desktop updater \"{_updater.Title}\": {exc}");
            }
        }

        public override async Task<bool> ApplyAsync(IUpgradeContext context, CancellationToken token)
        {
            var result = await base.ApplyAsync(context, token).ConfigureAwait(false);

            // Normally, the upgrader will apply steps one at a time
            // at the user's instruction. In the case of parent and child steps,
            // the parent has any top-level application done after the children.
            // In the case of the WinformsUpdateStep, the parent (this step's parent)
            // doesn't need to apply anything.
            // Therefore, automatically apply the parent ConfigUpdaterStep's updater
            // once all its children have been applied.
            if (_windowsDesktopUpdateStep.SubSteps.All(s => s.IsDone))
            {
                await _windowsDesktopUpdateStep.ApplyAsync(context, token).ConfigureAwait(false);
            }

            return result;
        }
    }
}

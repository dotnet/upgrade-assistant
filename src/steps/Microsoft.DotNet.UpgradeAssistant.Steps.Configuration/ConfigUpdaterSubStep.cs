﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration
{
    public class ConfigUpdaterSubStep : MigrationStep
    {
        private readonly ConfigUpdaterStep _parentStep;
        private readonly IConfigUpdater _configUpdater;

        public override string Id => _configUpdater.Id;

        public override string Description => _configUpdater.Description;

        public override string Title => _configUpdater.Title;

        public ConfigUpdaterSubStep(MigrationStep parentStep, IConfigUpdater configUpdater, ILogger logger)
            : base(logger)
        {
            _parentStep = (ParentStep = parentStep) as ConfigUpdaterStep ?? throw new ArgumentNullException(nameof(parentStep));
            _configUpdater = configUpdater ?? throw new ArgumentNullException(nameof(configUpdater));
        }

        protected override bool IsApplicableImpl(IMigrationContext context) => context?.CurrentProject is not null && (_parentStep?.ConfigFiles.Any() ?? false);

        protected override async Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                return await _configUpdater.ApplyAsync(context, _parentStep.ConfigFiles, token).ConfigureAwait(false)
                    ? new MigrationStepApplyResult(MigrationStepStatus.Complete, string.Empty)
                    : new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Failed to apply config updater \"{_configUpdater.Title}\"");
            }
            catch (Exception exc)
            {
                Logger.LogError(exc, "Unexpected exception while apply config updater \"{ConfigUpdater}\"", _configUpdater.Title);
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Unexpected exception while applying config updater \"{_configUpdater.Title}\": {exc}");
            }
        }

        public override async Task<bool> ApplyAsync(IMigrationContext context, CancellationToken token)
        {
            var result = await base.ApplyAsync(context, token).ConfigureAwait(false);

            // Normally, the migrator will apply steps one at a time
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

        protected override async Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                return await _configUpdater.IsApplicableAsync(context, _parentStep.ConfigFiles, token).ConfigureAwait(false)
                    ? new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, $"Config updater \"{_configUpdater.Title}\" needs applied", _configUpdater.Risk)
                    : new MigrationStepInitializeResult(MigrationStepStatus.Complete, string.Empty, BuildBreakRisk.None);
            }
            catch (Exception exc)
            {
                Logger.LogError(exc, "Unexpected exception while initializing config updater \"{ConfigUpdater}\"", _configUpdater.Title);
                return new MigrationStepInitializeResult(MigrationStepStatus.Failed, $"Unexpected exception while initializing config updater \"{_configUpdater.Title}\": {exc}", Risk);
            }
        }
    }
}

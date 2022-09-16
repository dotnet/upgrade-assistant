// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.TryConvert
{
    public class SetTFMStep : UpgradeStep
    {
        private readonly IPackageRestorer _restorer;
        private readonly ITargetFrameworkSelector _tfmSelector;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be SDK-style before changing package references
            WellKnownStepIds.TryConvertProjectConverterStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        public SetTFMStep(IPackageRestorer restorer, ITargetFrameworkSelector tfmSelector, ILogger<SetTFMStep> logger)
            : base(logger)
        {
            _restorer = restorer;
            _tfmSelector = tfmSelector;
        }

        public override string Title => "Update TFM";

        public override string Description => "Update TFM for current project";

        public override string Id => WellKnownStepIds.SetTFMStepId;

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            var targetTfm = await _tfmSelector.SelectTargetFrameworkAsync(project, token).ConfigureAwait(false);
            var file = project.GetFile();

            file.SetTFM(targetTfm);

            await file.SaveAsync(token).ConfigureAwait(false);

            // With an updated TFM, we should restore packages
            await _restorer.RestorePackagesAsync(context, context.CurrentProject.Required(), token).ConfigureAwait(false);

            var description = $"Updated TFM to {targetTfm}";
            Logger.LogInformation(description);
            AddResultToContext(context, UpgradeStepStatus.Complete, description);
            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Updated TFM to {targetTfm}");
        }

        private void AddResultToContext(IUpgradeContext context, UpgradeStepStatus status, string resultMessage)
        {
            context.AddResultForStep(this, context.CurrentProject?.GetFile()?.FilePath ?? string.Empty, status, resultMessage);
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();
            var targetTfm = await _tfmSelector.SelectTargetFrameworkAsync(project, token).ConfigureAwait(false);

            if (project.TargetFrameworks.Any(tfm => tfm == targetTfm))
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "TFM is already set to target value.", BuildBreakRisk.None);
            }
            else
            {
                Logger.LogInformation("TFM needs updated to {TargetTFM}", targetTfm);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"TFM needs to be updated to {targetTfm}", BuildBreakRisk.High);
            }
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is not null);
    }
}

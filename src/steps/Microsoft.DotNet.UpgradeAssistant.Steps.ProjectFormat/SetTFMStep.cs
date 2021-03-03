// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    public class SetTFMStep : UpgradeStep
    {
        private readonly IPackageRestorer _restorer;
        private readonly ITargetTFMSelector _tfmSelector;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be SDK-style before changing package references
            "Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat.TryConvertProjectConverterStep",
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.NextProjectStep",
        };

        public SetTFMStep(IPackageRestorer restorer, ITargetTFMSelector tfmSelector, ILogger<SetTFMStep> logger)
            : base(logger)
        {
            _restorer = restorer;
            _tfmSelector = tfmSelector;
        }

        public override string Title => "Update TFM";

        public override string Description => "Update TFM for current project";

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            var targetTfm = _tfmSelector.SelectTFM(project);
            var file = project.GetFile();

            file.SetTFM(targetTfm);

            await file.SaveAsync(token).ConfigureAwait(false);

            // With an updated TFM, we should restore packages
            await _restorer.RestorePackagesAsync(context, context.CurrentProject.Required(), token).ConfigureAwait(false);

            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Updated TFM to {targetTfm}");
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();
            var targetTfm = _tfmSelector.SelectTFM(project);

            if (targetTfm == project.TFM)
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "TFM is already set to target value.", BuildBreakRisk.None));
            }
            else
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"TFM needs to be updated to {targetTfm}", BuildBreakRisk.High));
            }
        }

        protected override bool IsApplicableImpl(IUpgradeContext context) => context?.CurrentProject is not null;
    }
}

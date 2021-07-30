// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public class MauiPlatformTargetFrameworkUpgradeStep : UpgradeStep
    {
        private readonly ITargetFrameworkSelector _tfmSelector;

        public override string Title => "Add TargetFramework for .NET MAUI Project";

        public override string Description => "Add Platform TargetFramework for XamarinForms projects being converted";

        public MauiPlatformTargetFrameworkUpgradeStep(ITargetFrameworkSelector tfmSelector, ILogger<MauiPlatformTargetFrameworkUpgradeStep> logger)
            : base(logger)
        {
            _tfmSelector = tfmSelector;
        }

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.BackupStepId,
            WellKnownStepIds.TryConvertProjectConverterStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
            WellKnownStepIds.SetTFMStepId,
        };

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var componentFlagProperty = context.Properties.GetPropertyValue("componentFlag");
            if (componentFlagProperty is null)
            {
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"componentFlag Context property was null.");
            }

            var targetTfm = GetExpectedTargetFramework(componentFlagProperty);

            var project = context.CurrentProject.Required();
            var file = project.GetFile();

            file.SetTFM(targetTfm);

            await file.SaveAsync(token).ConfigureAwait(false);

            Logger.LogInformation("Added TFM to {TargetTFM}", targetTfm);
            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Added TFM {targetTfm} to MAUI project ");
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            var componentFlagProperty = context.Properties.GetPropertyValue("componentFlag");
            if (componentFlagProperty is null)
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "componentFlag Property in Context was null", BuildBreakRisk.High));
            }

            var targetTfm = GetExpectedTargetFramework(componentFlagProperty);

            if (project.TargetFrameworks.Any(tfm => tfm == targetTfm))
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "TFM is already set to target value.", BuildBreakRisk.None));
            }
            else
            {
                Logger.LogInformation("TFM needs updated to {TargetTFM}", targetTfm);
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"TFM needs to be updated to {targetTfm}", BuildBreakRisk.High));
            }
        }

        private static TargetFrameworkMoniker GetExpectedTargetFramework(string componentFlagProperty)
        {
            var propertyValue = Enum.Parse(typeof(ProjectComponents), componentFlagProperty);
            if (ProjectComponents.XamarinAndroid.CompareTo(propertyValue) == 0)
            {
                return TargetFrameworkMoniker.Net60_Android;
            }
            else
            {
                return TargetFrameworkMoniker.Net60_iOS;
            }
        }

        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                return false;
            }

            if (context.CurrentProject is null)
            {
                return false;
            }

            var project = context.CurrentProject.Required();

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

            if (components.HasFlag(ProjectComponents.XamarinAndroid) || components.HasFlag(ProjectComponents.XamariniOS))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(context.Properties.GetPropertyValue("componentFlag")))
            {
                return true;
            }

            return false;
        }
    }
}

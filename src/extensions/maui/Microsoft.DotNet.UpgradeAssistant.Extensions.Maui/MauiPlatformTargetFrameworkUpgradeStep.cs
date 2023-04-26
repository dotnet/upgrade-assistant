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

            var project = context.CurrentProject.Required();
            var file = project.GetFile();
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

            // This block checks TFMs for .NET MAUI project
            if (components.HasFlag(ProjectComponents.Maui) && file.IsSdk)
            {
                if (project.IsNetStandard())
                {
                    var projectProperties = project.GetProjectPropertyElements();
                    projectProperties.RemoveProjectProperty("TargetFramework");
                    file.SetPropertyValue("UseMaui", "true");
                    file.SetPropertyValue("TargetFrameworks", "net7.0-android;net7.0-ios");
                    await file.SaveAsync(token).ConfigureAwait(false);
                    Logger.LogInformation("Added TFMs to .NET MAUI project {ProjectName}", project);
                    return context.CreateAndAddStepApplyResult(this, UpgradeStepStatus.Complete, $"Added TFMs to .NET MAUI project {project}");
                }
                else
                {
                    return context.CreateAndAddStepApplyResult(this, UpgradeStepStatus.Failed, $"Project {project} is not recognized as a .NET MAUI project (TargetFrameworks: {string.Join(", ", project.TargetFrameworks)})");
                }
            }

            // If we're getting here we are dealing with a head project, which received its "componentFlag" in the TryConvertRunner
            var componentFlagProperty = context.Properties.GetPropertyValue("componentFlag");
            var targetTfm = GetExpectedTargetFramework(componentFlagProperty);
            if (targetTfm is null)
            {
                return context.CreateAndAddStepApplyResult(this, UpgradeStepStatus.Failed, $"Failed to retrieve target TFM from component flag context property: '{componentFlagProperty}'");
            }

            file.SetTFM(targetTfm);
            await file.SaveAsync(token).ConfigureAwait(false);
            Logger.LogInformation("Added TFM {TargetTFM} to .NET MAUI head project {ProjectName}", targetTfm, project);
            return context.CreateAndAddStepApplyResult(this, UpgradeStepStatus.Complete, $"Added TFM {targetTfm} to .NET MAUI head project {project}");
        }

        protected async override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

            // This block checks TFMs for .NET MAUI Head Project
            if (components.HasFlag(ProjectComponents.Maui))
            {
                if (project.IsNetStandard())
                {
                    Logger.LogInformation("TFM needs to be updated to .NET MAUI TFMs");

                    return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "TFM needs to be updated to .NET MAUI TargetFrameworks", BuildBreakRisk.High);
                }
                else
                {
                    return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "TFM is already set to target value.", BuildBreakRisk.None);
                }
            }

            var componentFlagProperty = context.Properties.GetPropertyValue("componentFlag");

            // This block checks TFMs for .NET MAUI platform projects
            var targetTfm = GetExpectedTargetFramework(componentFlagProperty);
            if (targetTfm is null)
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"Failed to retrieve target TFM from component flag context property: '{componentFlagProperty}'", BuildBreakRisk.High);
            }

            if (project.TargetFrameworks.Any(tfm => tfm == targetTfm))
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "TFM is already set to target value.", BuildBreakRisk.None);
            }
            else
            {
                Logger.LogInformation("TFM needs to be updated to {TargetTFM}", targetTfm);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"TFM needs to be updated to {targetTfm}", BuildBreakRisk.High);
            }
        }

        private static TargetFrameworkMoniker? GetExpectedTargetFramework(string? componentFlagProperty)
        {
            if (Enum.TryParse<ProjectComponents>(componentFlagProperty, out var propertyValue))
            {
                if (propertyValue.HasFlag(ProjectComponents.XamarinAndroid))
                {
                    return TargetFrameworkMoniker.Net70_Android;
                }
                else
                {
                    return TargetFrameworkMoniker.Net70_iOS;
                }
            }

            return null;
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

            if (components.HasFlag(ProjectComponents.MauiAndroid) || components.HasFlag(ProjectComponents.MauiiOS) || components.HasFlag(ProjectComponents.Maui))
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

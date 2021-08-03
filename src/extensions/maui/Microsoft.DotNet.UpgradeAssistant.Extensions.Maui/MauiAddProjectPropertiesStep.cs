// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public class MauiAddProjectPropertiesStep : UpgradeStep
    {
        public override string Title => "Add Project Properties for .NET MAUI Project";

        public override string Description => "Adds the Project Properties per platform for .NET MAUI Projects";

        public MauiAddProjectPropertiesStep(ILogger<MauiAddProjectPropertiesStep> logger)
            : base(logger)
        {
        }

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.BackupStepId,
            WellKnownStepIds.TryConvertProjectConverterStepId,
            WellKnownStepIds.SetTFMStepId,
            WellKnownStepIds.PackageUpdaterStepId
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
            WellKnownStepIds.TemplateInserterStepId
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
            var types = project.ProjectTypes;
            if (components.HasFlag(ProjectComponents.MauiAndroid))
            {
                // add runtimeidentifier
            }

            if (components.HasFlag(ProjectComponents.MauiiOS))
            {
                // add runtimeidentifier
            }

            // Use MAUI tag
            file.SetPropertyValue("UseMaui", "true");
            await file.SaveAsync(token).ConfigureAwait(false);

            Logger.LogInformation("Added TFM to {TargetTFM}", "prop");

            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Added Project Properties for {components.ToString()} to MAUI project ");
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();
            var types = project.ProjectTypes;

            return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"TFM needs to be updated to {project}", BuildBreakRisk.High));
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
            if (components.HasFlag(ProjectComponents.MauiAndroid) || components.HasFlag(ProjectComponents.MauiiOS))
            {
                return true;
            }

            return false;
        }
    }
}

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
            var projectproperties = project.GetProjectPropertyElements();

            var projectType = await MauiUtilties.GetMauiProjectTypeForProject(project, token).ConfigureAwait(false);

            switch (projectType)
            {
                case MauiProjectType.Maui:
                    {
                        UpgradeMaui(projectproperties, file);
                        break;
                    }

                case MauiProjectType.MauiAndroid:
                    {
                        UpgradeMauiAndroid(projectproperties, file);
                        break;
                    }

                case MauiProjectType.MauiiOS:
                    {
                        UpgradeMauiiOS(projectproperties, file);
                        break;
                    }
            }

            if (projectproperties.GetProjectPropertyValue("UseMaui")?.FirstOrDefault() is null)
            {
                file.SetPropertyValue("UseMaui", "true");
            }

            await file.SaveAsync(token).ConfigureAwait(false);
            Logger.LogInformation("Added .NET MAUI Project Properties successfully");
            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Added Project Properties for {projectType.ToString()} to .NET MAUI project ");
        }

        private static void UpgradeMauiAndroid(IProjectPropertyElements projectproperties, IProjectFile file)
        {
            // confirm final mappings https://github.com/xamarin/xamarin-android/blob/main/Documentation/guides/OneDotNet.md#changes-to-msbuild-properties
            // remove uneeded properties
            projectproperties.RemoveProjectProperty("AndroidApplication");
            projectproperties.RemoveProjectProperty("AndroidResgenFile");
            projectproperties.RemoveProjectProperty("AndroidResgenClass");
            projectproperties.RemoveProjectProperty("MonoAndroidAssetsPrefix");
            projectproperties.RemoveProjectProperty("MonoAndroidAssetsPrefix");
            projectproperties.RemoveProjectProperty("AndroidUseLatestPlatformSdk");
            projectproperties.RemoveProjectProperty("AndroidEnableSGenConcurrent");
            projectproperties.RemoveProjectProperty("AndroidHttpClientHandlerType");
            projectproperties.RemoveProjectProperty("AndroidManagedSymbols");
            projectproperties.RemoveProjectProperty("AndroidUseSharedRuntime");
            projectproperties.RemoveProjectProperty("MonoAndroidResourcePrefix");
            projectproperties.RemoveProjectProperty("AndroidUseAapt2");
            projectproperties.RemoveProjectProperty("AndroidManifest");
            projectproperties.RemoveProjectProperty("AndroidSupportedAbis");

            var androidLinkMode = projectproperties.GetProjectPropertyValue("AndroidLinkMode");
            foreach (var linkMode in androidLinkMode)
            {
                projectproperties.RemoveProjectProperty("AndroidLinkMode");
                if (string.Equals(linkMode, "SdkOnly", StringComparison.Ordinal) || string.Equals(linkMode, "Full", StringComparison.Ordinal))
                {
                    file.SetPropertyValue("PublishTrimmed", "true");
                    file.SetPropertyValue("TrimMode", "link");
                }
            }

            file.SetPropertyValue("ImplicitUsings", "enable");
        }

        private static void UpgradeMauiiOS(IProjectPropertyElements projectproperties, IProjectFile file)
        {
            MauiUtilties.RuntimePropertyMapper(projectproperties, file, "MtouchArch");
            MauiUtilties.TransformProperty(projectproperties, file, "MtouchEnableSGenConc", "EnableSGenConc");

            // remove unneeded Properties
            projectproperties.RemoveProjectProperty("IPhoneResourcePrefix");
            projectproperties.RemoveProjectProperty("RuntimeIdentifiers");
            projectproperties.RemoveProjectProperty("EnableSGenConc");
            projectproperties.RemoveProjectProperty("ProvisioningType");
            projectproperties.RemoveProjectProperty("MtouchLink");
            projectproperties.RemoveProjectProperty("MtouchDebug");
            projectproperties.RemoveProjectProperty("MtouchInterpreter");
        }

        private static void UpgradeMaui(IProjectPropertyElements projectproperties, IProjectFile file)
        {
            // remove unneeded Properties
            projectproperties.RemoveProjectProperty("DebugType");
            projectproperties.RemoveProjectProperty("DebugSymbols");
            projectproperties.RemoveProjectProperty("ProduceReferenceAssembly");

            // adding MAUI Properties
            file.SetPropertyValue("OutputType", "Library");
            file.SetPropertyValue("ImplicitUsings", "enable");
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();
            var file = project.GetFile();
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            bool propertiesValueUpdated = true;

            // check project properties updated based on project types
            if (components.HasFlag(ProjectComponents.Maui))
            {
                if (string.IsNullOrEmpty(file.GetPropertyValue("ImplicitUsings")))
                {
                    propertiesValueUpdated = false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(file.GetPropertyValue("UseMaui")))
                {
                    propertiesValueUpdated = false;
                }
            }

            if (propertiesValueUpdated)
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, ".NET MAUI Project Properties already added.", BuildBreakRisk.None);
            }
            else
            {
                Logger.LogInformation(".NET MAUI Project Properties need to be added.");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, ".NET MAUI Project Properties need to be added", BuildBreakRisk.High);
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
            if (components.HasFlag(ProjectComponents.MauiAndroid) || components.HasFlag(ProjectComponents.MauiiOS) || components.HasFlag(ProjectComponents.Maui))
            {
                return true;
            }

            return false;
        }
    }
}

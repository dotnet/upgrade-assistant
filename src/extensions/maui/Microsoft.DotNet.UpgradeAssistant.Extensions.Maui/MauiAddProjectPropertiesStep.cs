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
        public override string Title => "Update Project Properties for .NET MAUI Project";

        public override string Description => "Updates the platform-specific project properties for a .NET MAUI project";

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
            var projectProperties = project.GetProjectPropertyElements();
            var projectType = await project.GetMauiProjectType(token).ConfigureAwait(false);

            switch (projectType)
            {
                case MauiProjectType.Maui:
                    {
                        UpgradeMaui(projectProperties, file);
                        break;
                    }

                case MauiProjectType.MauiAndroid:
                    {
                        UpgradeMauiAndroid(projectProperties, file);
                        break;
                    }

                case MauiProjectType.MauiiOS:
                    {
                        UpgradeMauiiOS(projectProperties, file);
                        break;
                    }
            }

            if (projectProperties.GetProjectPropertyValue("UseMaui")?.FirstOrDefault() is null)
            {
                file.SetPropertyValue("UseMaui", "true");
            }

            await file.SaveAsync(token).ConfigureAwait(false);
            Logger.LogInformation("Updated {ProjectType} project properties for .NET MAUI project {ProjectName}", projectType, project.FileInfo.Name);
            return context.CreateAndAddStepApplyResult(this, UpgradeStepStatus.Complete, $"Updated {projectType} project properties for .NET MAUI project {project.FileInfo.Name}");
        }

        private static void UpgradeMauiAndroid(IProjectPropertyElements projectProperties, IProjectFile file)
        {
            // confirm final mappings https://github.com/xamarin/xamarin-android/blob/main/Documentation/guides/OneDotNet.md#changes-to-msbuild-properties
            // remove unneeded properties
            projectProperties.RemoveProjectProperty("AndroidApplication");
            projectProperties.RemoveProjectProperty("AndroidResgenFile");
            projectProperties.RemoveProjectProperty("AndroidResgenClass");
            projectProperties.RemoveProjectProperty("MonoAndroidAssetsPrefix");
            projectProperties.RemoveProjectProperty("MonoAndroidAssetsPrefix");
            projectProperties.RemoveProjectProperty("AndroidUseLatestPlatformSdk");
            projectProperties.RemoveProjectProperty("AndroidEnableSGenConcurrent");
            projectProperties.RemoveProjectProperty("AndroidHttpClientHandlerType");
            projectProperties.RemoveProjectProperty("AndroidManagedSymbols");
            projectProperties.RemoveProjectProperty("AndroidUseSharedRuntime");
            projectProperties.RemoveProjectProperty("MonoAndroidResourcePrefix");
            projectProperties.RemoveProjectProperty("AndroidUseAapt2");
            projectProperties.RemoveProjectProperty("AndroidManifest");
            projectProperties.RemoveProjectProperty("AndroidSupportedAbis");

            var androidLinkMode = projectProperties.GetProjectPropertyValue("AndroidLinkMode");
            foreach (var linkMode in androidLinkMode)
            {
                projectProperties.RemoveProjectProperty("AndroidLinkMode");
                if (string.Equals(linkMode, "SdkOnly", StringComparison.Ordinal) || string.Equals(linkMode, "Full", StringComparison.Ordinal))
                {
                    file.SetPropertyValue("PublishTrimmed", "true");
                    file.SetPropertyValue("TrimMode", "link");
                }
            }

            file.SetPropertyValue("ImplicitUsings", "enable");
        }

        private static void UpgradeMauiiOS(IProjectPropertyElements projectProperties, IProjectFile file)
        {
            MauiUtilities.RuntimePropertyMapper(projectProperties, file, "MtouchArch");
            MauiUtilities.TransformProperty(projectProperties, file, "MtouchEnableSGenConc", "EnableSGenConc");

            // remove unneeded Properties
            projectProperties.RemoveProjectProperty("IPhoneResourcePrefix");
            projectProperties.RemoveProjectProperty("RuntimeIdentifiers");
            projectProperties.RemoveProjectProperty("EnableSGenConc");
            projectProperties.RemoveProjectProperty("ProvisioningType");
            projectProperties.RemoveProjectProperty("MtouchLink");
            projectProperties.RemoveProjectProperty("MtouchDebug");
            projectProperties.RemoveProjectProperty("MtouchInterpreter");
        }

        private static void UpgradeMaui(IProjectPropertyElements projectProperties, IProjectFile file)
        {
            // remove unneeded Properties
            projectProperties.RemoveProjectProperty("DebugType");
            projectProperties.RemoveProjectProperty("DebugSymbols");
            projectProperties.RemoveProjectProperty("ProduceReferenceAssembly");

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

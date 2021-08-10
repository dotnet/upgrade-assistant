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

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (components.HasFlag(ProjectComponents.MauiAndroid))
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

                projectproperties.RemoveProjectProperty("AndroidSupportedAbis");
                file.SetPropertyValue("RuntimeIdentifiers", "android-arm;android-arm64;android-x86;android-x64");

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
            }

            if (components.HasFlag(ProjectComponents.MauiiOS))
            {
                // following conversion mapping here : https://github.com/xamarin/xamarin-macios/wiki/Project-file-properties-dotnet-migration
                var runtimeprops = projectproperties.GetProjectPropertyValue("MtouchArch").Distinct();
                var runtimeIdentifierString = string.Empty;
                int count = 0;
                foreach (var prop in runtimeprops)
                {
                    if (string.Equals(prop, "x86_64", StringComparison.Ordinal))
                    {
                        runtimeIdentifierString += "iossimulator-x64;";
                        count++;
                    }

                    if (string.Equals(prop, "i386", StringComparison.Ordinal))
                    {
                        runtimeIdentifierString += "iossimulator-x86;";
                        count++;
                    }

                    if (string.Equals(prop, "ARM64", StringComparison.Ordinal))
                    {
                        runtimeIdentifierString += "ios-arm64;";
                        count++;
                    }

                    if (string.Equals(prop, "x86_64+i386", StringComparison.Ordinal))
                    {
                        runtimeIdentifierString += "iossimulator-x86;iossimulator-x64;";
                        count++;
                    }

                    if (string.Equals(prop, "ARMv7+ARM64+i386", StringComparison.Ordinal))
                    {
                        runtimeIdentifierString += "ios-arm;ios-arm64;";
                        count++;
                    }
                }

                // remove old properties before adding new
                projectproperties.RemoveProjectProperty("MtouchArch");
                if (count > 1)
                {
                    file.SetPropertyValue("RuntimeIdentifiers", runtimeIdentifierString);
                }
                else
                {
                    file.SetPropertyValue("RuntimeIdentifier", runtimeIdentifierString);
                }

                var enablesgen = projectproperties.GetProjectPropertyValue("MtouchEnableSGenConc").FirstOrDefault();
                if (string.IsNullOrEmpty(enablesgen))
                {
                    projectproperties.RemoveProjectProperty("MtouchEnableSGenConc");
                    file.SetPropertyValue("EnableSGenConc", enablesgen);
                }

                var mtouchhttpclienthandler = projectproperties.GetProjectPropertyValue("MtouchHttpClientHandler").FirstOrDefault();
                if (string.IsNullOrEmpty(mtouchhttpclienthandler))
                {
                    projectproperties.RemoveProjectProperty("MtouchHttpClientHandler");

                    if (string.Equals(mtouchhttpclienthandler, "HttpClientHandler", StringComparison.Ordinal))
                    {
                        file.SetPropertyValue("UseNativeHttpHandler", "false");
                    }
                }

                var httpclienthandler = projectproperties.GetProjectPropertyValue("HttpClientHandler").FirstOrDefault();
                if (string.IsNullOrEmpty(httpclienthandler))
                {
                    projectproperties.RemoveProjectProperty("MtouchHttpClientHandler");

                    if (string.Equals(httpclienthandler, "HttpClientHandler", StringComparison.Ordinal))
                    {
                        file.SetPropertyValue("UseNativeHttpHandler", "false");
                    }
                }

                // remove unneeded Properties
                projectproperties.RemoveProjectProperty("IPhoneResourcePrefix");
            }

            if (components.HasFlag(ProjectComponents.Maui))
            {
                // remove unneeded Properties
                projectproperties.RemoveProjectProperty("DebugType");
                projectproperties.RemoveProjectProperty("DebugSymbols");

            }

            // Use MAUI tag
            file.SetPropertyValue("UseMaui", "true");
            await file.SaveAsync(token).ConfigureAwait(false);

            Logger.LogInformation("Added .NET MAUI Project Properties successfully");
            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Added Project Properties for {components.ToString()} to .NET MAUI project ");
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();
            var file = project.GetFile();

            // var properties =  project.GetProjectPropertyElements();
            if (string.IsNullOrEmpty(file.GetPropertyValue("UseMaui")))
            {
                Logger.LogInformation(".NET MAUI Project Properties need to be added.");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, ".NET MAUI Project Properties need to be added", BuildBreakRisk.High));
            }
            else
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, ".NET MAUI Project Properties already added.", BuildBreakRisk.None));
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

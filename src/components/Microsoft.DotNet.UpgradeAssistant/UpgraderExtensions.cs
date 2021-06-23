// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.UpgradeAssistant.Checks;
using Microsoft.DotNet.UpgradeAssistant.TargetFramework;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class UpgraderExtensions
    {
        public static void AddStepManagement(this IServiceCollection services, Action<DefaultTfmOptions> options)
        {
            services.AddScoped<UpgraderManager>();
            services.AddTransient<IUpgradeContextProperties, UpgradeContextProperties>();
            services.AddTransient<IUpgradeStepOrderer, UpgradeStepOrderer>();

            services.AddReadinessChecks();
            services.AddTargetFrameworkSelectors();

            services.AddOptions<DefaultTfmOptions>()
                .Configure(options)
                .ValidateDataAnnotations();
            services.AddContextTelemetry();
        }

        public static void AddContextTelemetry(this IServiceCollection services)
        {
            services.AddSingleton<UpgradeContextTelemetry>();
            services.AddTransient<ITelemetryInitializer>(ctx => ctx.GetRequiredService<UpgradeContextTelemetry>());
            services.AddTransient<IUpgradeContextAccessor>(ctx => ctx.GetRequiredService<UpgradeContextTelemetry>());
        }

        private static void AddReadinessChecks(this IServiceCollection services)
        {
            services.AddTransient<IUpgradeReadyCheck, CanLoadProjectFile>();
            services.AddTransient<IUpgradeReadyCheck, CentralPackageManagementCheck>();
            services.AddTransient<IUpgradeReadyCheck, MultiTargetFrameworkCheck>();

            services.AddTransient<IUpgradeReadyCheck, WebFormsCheck>();
            services.AddTransient<IUpgradeReadyCheck, WcfServerCheck>();

            services.AddOptions<UpgradeReadinessOptions>()
                .Configure<UpgradeOptions>((upgradeReadinessOptions, upgradeOptions) =>
                {
                    upgradeReadinessOptions.IgnoreUnsupportedFeatures = upgradeOptions.IgnoreUnsupportedFeatures;
                });
        }

        private static void AddTargetFrameworkSelectors(this IServiceCollection services)
        {
            services.AddTransient<ITargetFrameworkSelector, TargetFrameworkSelector>();

            services.AddTransient<ITargetFrameworkSelectorFilter, DependencyMinimumTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, WebProjectTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, WindowsSdkTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, ExecutableTargetFrameworkSelectorFilter>();
        }
    }
}

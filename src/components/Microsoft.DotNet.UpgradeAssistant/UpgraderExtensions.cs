// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.DotNet.UpgradeAssistant.Checks;
using Microsoft.DotNet.UpgradeAssistant.TargetFramework;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class UpgraderExtensions
    {
        public static void AddStepManagement(this IServiceCollection services)
        {
            services.AddScoped<UpgraderManager>();
            services.AddTransient<IUpgradeStepOrderer, UpgradeStepOrderer>();

            services.AddContextTelemetry();
        }

        public static void AddContextTelemetry(this IServiceCollection services)
        {
            services.AddSingleton<UpgradeContextTelemetry>();
            services.AddTransient<ITelemetryInitializer>(ctx => ctx.GetRequiredService<UpgradeContextTelemetry>());
            services.AddTransient<IUpgradeContextAccessor>(ctx => ctx.GetRequiredService<UpgradeContextTelemetry>());
        }

        public static void AddReadinessChecks(this IServiceCollection services, Action<UpgradeReadinessOptions> configure)
        {
            services.AddTransient<IUpgradeReadyCheck, CanLoadProjectFile>();
            services.AddTransient<IUpgradeReadyCheck, CentralPackageManagementCheck>();
            services.AddTransient<IUpgradeReadyCheck, MultiTargetFrameworkCheck>();

            services.AddTransient<IUpgradeReadyCheck, WebFormsCheck>();
            services.AddTransient<IUpgradeReadyCheck, WcfServerCheck>();

            services.AddOptions<UpgradeReadinessOptions>()
                .Configure(configure);
        }

        public static void AddTargetFrameworkSelectors(this IServiceCollection services, Action<DefaultTfmOptions> configure)
        {
            services.AddOptions<DefaultTfmOptions>()
                .Configure(configure)
                .ValidateDataAnnotations();

            services.AddTransient<ITargetFrameworkSelector, TargetFrameworkSelector>();

            services.AddTransient<ITargetFrameworkSelectorFilter, DependencyMinimumTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, ExecutableTargetFrameworkSelectorFilter>();
        }

        public static void ConfigureOutputOptions(this IServiceCollection services, Action<OutputOptions> configure)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions<OutputOptions>()
                .Configure(configure)
                .ValidateDataAnnotations();
        }
    }
}

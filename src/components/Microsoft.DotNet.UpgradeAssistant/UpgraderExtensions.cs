// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Checks;
using Microsoft.DotNet.UpgradeAssistant.TargetFramework;
using Microsoft.DotNet.UpgradeAssistant.VisualBasic;
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
        }

        private static void AddReadinessChecks(this IServiceCollection services)
        {
            services.AddTransient<IUpgradeReadyCheck, CanLoadProjectFile>();
            services.AddTransient<IUpgradeReadyCheck, CentralPackageManagementCheck>();
            services.AddTransient<IUpgradeReadyCheck, TargetFrameworkCheck>();
            services.AddTransient<IUpgradeReadyCheck, VisualBasicRazorTemplateCheck>();
            services.AddTransient<IUpgradeReadyCheck, WebFormsCheck>();
        }

        private static void AddTargetFrameworkSelectors(this IServiceCollection services)
        {
            services.AddTransient<ITargetFrameworkSelector, TargetFrameworkSelector>();

            services.AddTransient<ITargetFrameworkSelectorFilter, DependencyMinimumTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, WebProjectTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, WindowsSdkTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, ExecutableTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, MyTypeTargetFrameworkSelectorFilter>();
        }
    }
}

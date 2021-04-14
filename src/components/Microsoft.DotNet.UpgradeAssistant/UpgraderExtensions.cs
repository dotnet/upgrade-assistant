// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            services.AddReadinessChecks();
            services.AddTargetFrameworkSelectors();
        }

        public static void AddReadinessChecks(this IServiceCollection services)
        {
            services.AddTransient<IUpgradeReadyCheck, CanLoadProjectFile>();
            services.AddTransient<IUpgradeReadyCheck, CentralPackageManagementCheck>();
            services.AddTransient<IUpgradeReadyCheck, TargetFrameworkCheck>();
        }

        public static void AddTargetFrameworkSelectors(this IServiceCollection services)
        {
            services.AddTransient<ITargetFrameworkSelector, TargetFrameworkSelector>();

            services.AddTransient<ITargetFrameworkSelectorFilter, SatisifiesProjectDependenciesTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, WebProjectTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, WindowsSdkTargetFrameworkSelectorFilter>();
            services.AddTransient<ITargetFrameworkSelectorFilter, ExecutableTargetFrameworkSelectorFilter>();
        }
    }
}

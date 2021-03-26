// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Checks;
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
        }

        public static void AddReadinessChecks(this IServiceCollection services)
        {
            services.AddTransient<IUpgradeReadyCheck, CanLoadProjectFile>();
            services.AddTransient<IUpgradeReadyCheck, VisualBasicWpfCheck>();
            services.AddTransient<IUpgradeReadyCheck, CentralPackageManagementCheck>();
            services.AddTransient<IUpgradeReadyCheck, TargetFrameworkCheck>();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Migrator;
using Microsoft.DotNet.UpgradeAssistant.Migrator.Steps;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class MigratorExtensions
    {
        public static void AddStepManagement(this IServiceCollection services)
        {
            services.AddScoped<MigratorManager>();
            services.AddTransient<IMigrationStepOrderer, MigrationStepOrderer>();
            services.AddScoped<MigrationStep, NextProjectStep>();
            services.AddScoped<MigrationStep, SolutionCompletedStep>();
        }
    }
}

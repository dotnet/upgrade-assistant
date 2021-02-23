// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Steps.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class ConfigUpdaterStepExtensions
    {
        private const string ConfigUpdaterOptionsSectionName = "ConfigUpdater";

        public static IServiceCollection AddConfigUpdaterStep(this IServiceCollection services)
        {
            services.AddScoped<UpgradeStep, ConfigUpdaterStep>();

            // Register config updater options from the aggregate extension so that other
            // extensions have an opportunity to add their own config file paths.
            services.AddTransient(sp =>
            {
                var extensions = sp.GetRequiredService<AggregateExtension>();
                return extensions.GetOptions<ConfigUpdaterOptions>(ConfigUpdaterOptionsSectionName)
                    ?? new ConfigUpdaterOptions();
            });

            return services;
        }
    }
}

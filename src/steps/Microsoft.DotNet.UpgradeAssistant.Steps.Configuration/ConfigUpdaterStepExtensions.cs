// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Steps.Configuration;
using Microsoft.DotNet.UpgradeAssistant.Steps.Configuration.Updaters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class ConfigUpdaterStepExtensions
    {
        private const string ConfigUpdaterOptionsSectionName = "ConfigUpdater";

        public static void AddConfigUpdaterStep(this IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Services.AddUpgradeStep<ConfigUpdaterStep>();
            services.AddExtensionOption<ConfigUpdaterOptions>(ConfigUpdaterOptionsSectionName);
            services.Services.AddDefaultConfigUpdaters();
        }

        private static void AddDefaultConfigUpdaters(this IServiceCollection services)
        {
            services.AddScoped<IUpdater<ConfigFile>, ConnectionStringsConfigUpdater>();
            services.AddScoped<IUpdater<ConfigFile>, AppSettingsConfigUpdater>();
            services.AddScoped<IUpdater<ConfigFile>, UnsupportedSectionConfigUpdater>();
        }
    }
}

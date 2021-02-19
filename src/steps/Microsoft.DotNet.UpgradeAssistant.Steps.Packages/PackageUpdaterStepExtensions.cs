﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class PackageUpdaterStepExtensions
    {
        public static OptionsBuilder<PackageUpdaterOptions> AddPackageUpdaterStep(this IServiceCollection services)
        {
            services.AddSingleton<PackageMapProvider>();
            services.AddScoped<MigrationStep, PackageUpdaterStep>();
            services.AddSingleton<IPackageLoader, PackageLoader>();

            return services.AddOptions<PackageUpdaterOptions>();
        }
    }
}

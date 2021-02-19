// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Steps
{
    public static class PackageUpdaterStepExtensions
    {
        public static OptionsBuilder<PackageUpdaterOptions> AddPackageUpdaterStep(this IServiceCollection services)
        {
            // Add package analyzers (note that the order matters as the analyzers are run in the order registered)
            services.AddTransient<IPackageReferencesAnalyzer, DuplicateReferenceAnalyzer>();
            services.AddTransient<IPackageReferencesAnalyzer, TransitiveReferenceAnalyzer>();
            services.AddTransient<IPackageReferencesAnalyzer, PackageMapReferenceAnalyzer>();
            services.AddTransient<IPackageReferencesAnalyzer, TargetCompatibilityReferenceAnalyzer>();
            services.AddTransient<IPackageReferencesAnalyzer, UpgradeAssistantReferenceAnalyzer>();
            services.AddTransient<IPackageReferencesAnalyzer, WindowsCompatReferenceAnalyzer>();

            services.AddSingleton<PackageMapProvider>();
            services.AddSingleton<ITargetFrameworkMonikerComparer, TargetFrameworkMonikerComparer>();
            services.AddScoped<MigrationStep, PackageUpdaterStep>();
            return services.AddOptions<PackageUpdaterOptions>();
        }
    }
}

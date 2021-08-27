// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.MSBuild;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Configuration;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class NuGetExtensionBuilder : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
            => AddNuGet(services.Services);

        private static void AddNuGet(IServiceCollection services)
        {
            services.AddTransient<ITargetFrameworkCollection, TargetFrameworkMonikerCollection>();
            services.AddTransient<INuGetReferences, ProjectNuGetReferences>();
            services.AddSingleton<PackageLoader>();
            services.AddTransient<IPackageLoader>(ctx => ctx.GetRequiredService<PackageLoader>());
            services.AddTransient<IPackageDownloader>(ctx => ctx.GetRequiredService<PackageLoader>());
            services.AddSingleton<IVersionComparer, NuGetVersionComparer>();
            services.AddTransient<ITargetFrameworkMonikerComparer, NuGetTargetFrameworkMonikerComparer>();
            services.AddSingleton<IUpgradeStartup, NuGetCredentialsStartup>();
            services.AddSingleton<INuGetPackageSourceFactory, NuGetPackageSourceFactory>();
            services.AddOptions<NuGetDownloaderOptions>()
                .Configure(options =>
                {
                    var settings = Settings.LoadDefaultSettings(null);
                    options.CachePath = SettingsUtility.GetGlobalPackagesFolder(settings);
                });
        }
    }
}

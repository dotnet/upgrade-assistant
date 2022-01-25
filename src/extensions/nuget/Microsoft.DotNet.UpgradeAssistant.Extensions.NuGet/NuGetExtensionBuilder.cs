// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class NuGetExtensionBuilder : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            AddNuGet(services.Services);
        }

        private static void AddNuGet(IServiceCollection services)
        {
            services.AddTransient<ITransitiveDependencyIdentifier, NuGetTransitiveDependencyIdentifier>();
            services.AddTransient<ITargetFrameworkCollection, TargetFrameworkMonikerCollection>();
            services.AddTransient<INuGetReferences, ProjectNuGetReferences>();
            services.AddSingleton<PackageLoader>();
            services.AddTransient<IPackageLoader>(ctx => ctx.GetRequiredService<PackageLoader>());
            services.AddTransient<IPackageDownloader>(ctx => ctx.GetRequiredService<PackageLoader>());
            services.AddTransient<IPackageCreator>(ctx => ctx.GetRequiredService<PackageLoader>());
            services.AddTransient<IPackageSearch, HttpPackageSearch>();
            services.AddSingleton<IVersionComparer, NuGetVersionComparer>();
            services.AddTransient<ITargetFrameworkMonikerComparer, NuGetTargetFrameworkMonikerComparer>();
            services.AddSingleton<IUpgradeStartup, NuGetCredentialsStartup>();
            services.AddSingleton<INuGetPackageSourceFactory, NuGetPackageSourceFactory>();
            services.AddSingleton(_ => Settings.LoadDefaultSettings(null));
            services.AddSingleton(_ => new SourceCacheContext { NoCache = true });
            services.AddSingleton(ctx => ctx.GetRequiredService<INuGetPackageSourceFactory>().GetPackageSources(ctx.GetRequiredService<IOptions<NuGetDownloaderOptions>>().Value.PackageSourcePath));
            services.AddOptions<NuGetDownloaderOptions>()
                .Configure<ISettings>((options, settings) =>
                {
                    options.CachePath = SettingsUtility.GetGlobalPackagesFolder(settings);
                });
        }
    }
}

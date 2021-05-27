// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class PackageUpdaterStepExtensions
    {
        private const string PackageMapExtension = "*.json";
        private const string PackageUpdaterOptionsSectionName = "PackageUpdater";

        public static void AddPackageUpdaterStep(this IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new System.ArgumentNullException(nameof(services));
            }

            services.Services.AddUpgradeStep<PackageUpdaterPreTFMStep>();
            services.Services.AddUpgradeStep<PackageUpdaterStep>();
            services.Services.AddTransient<IDependencyAnalyzerRunner, DependencyAnalyzerRunner>();
            services.Services.AddTransient<IAnalyzeResultProvider, AnalyzePackageStatus>();

            services.AddExtensionOption<PackageUpdaterOptions>(PackageUpdaterOptionsSectionName)
                .MapFiles<NuGetPackageMap[]>(t => Path.Combine(t.PackageMapPath, PackageMapExtension));
        }
    }
}

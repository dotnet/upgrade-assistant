// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    /// <summary>
    /// Extension class with methods for registering WinformsUpdaterStep-related services.
    /// </summary>
    public static class WinformsUpdaterStepExtensions
    {
        /// <summary>
        /// Extension method for registering WinformsUpdaterStep and related services.
        /// </summary>
        /// <param name="services">The IServiceCollection to register services in.</param>
        /// <returns>The services argument updated with WinformsUpdaterStep and related services included.</returns>
        public static IServiceCollection AddWinformsUpdaterStep(this IServiceCollection services) =>
            services
            .AddUpgradeStep<WinformsUpdateStep>()
            .AddTransient<IUpdater<IProject>, WinformsDefaultFontUpdater>()
            .AddTransient<IUpdater<IProject>, WinformsDpiSettingUpdater>()
            .AddTransient<IUpdater<IProject>, WinUINamespaceUpdater>()
            .AddTransient<IUpdater<IProject>, WinUIPropertiesUpdater>()
            .AddTransient<IUpdater<IProject>, WinUIPackageAppxmanifestUpdater>()
            .AddTransient<IUpdater<IProject>, WinUIUnnecessaryFilesUpdater>();
    }
}

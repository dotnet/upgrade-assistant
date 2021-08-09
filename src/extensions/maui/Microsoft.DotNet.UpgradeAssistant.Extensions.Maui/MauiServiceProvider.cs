// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public class MauiServiceProvider : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Services.AddTransient<IUpgradeReadyCheck, XamarinFormsCheck>();
            services.Services.AddTransient<ITargetFrameworkSelectorFilter, MauiTargetFrameworkSelectorFilter>();
            services.Services.AddTransient<IComponentIdentifier, MauiComponentIdentifier>();
            services.Services.AddUpgradeStep<MauiPlatformTargetFrameworkUpgradeStep>();
        }
    }
}

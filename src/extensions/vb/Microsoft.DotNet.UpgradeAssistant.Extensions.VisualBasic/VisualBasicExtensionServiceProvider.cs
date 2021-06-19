// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic
{
    public class VisualBasicExtensionServiceProvider : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Services.AddUpgradeStep<VisualBasicProjectUpdaterStep>();
            services.Services.AddTransient<IDependencyAnalyzer, MyDotAnalyzer>();
            services.Services.AddTransient<ITargetFrameworkSelectorFilter, MyTypeTargetFrameworkSelectorFilter>();
        }
    }
}

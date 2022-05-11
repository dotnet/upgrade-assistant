// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public class WindowsServiceProvider : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Services.AddTransient<ITargetFrameworkSelectorFilter, WindowsSdkTargetFrameworkSelectorFilter>();
            services.Services.AddTransient<IComponentIdentifier, WindowsComponentIdentifier>();
            services.Services.AddTransient<IAnalyzeResultProvider, WinformsResultProvider>();
            services.Services.AddUpgradeStep<WindowsDesktopUpdateStep>();
            services.AddExtensionOption<WinUIOptions>(WinUIOptions.Name);
            services.Services.AddWinformsUpdaterStep();
            services.Services.AddWinUIUpdateSteps();
        }
    }
}

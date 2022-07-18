// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
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

            services.Services.AddTransient<IUpgradeReadyCheck, XamarinFormsVersionCheck>();
            services.Services.AddTransient<ITargetFrameworkSelectorFilter, MauiTargetFrameworkSelectorFilter>();

            // Disabled due to https://github.com/dotnet/upgrade-assistant/issues/1208
            // services.Services.AddTransient<IComponentIdentifier, MauiComponentIdentifier>();
            services.Services.AddUpgradeStep<MauiPlatformTargetFrameworkUpgradeStep>();
            services.Services.AddUpgradeStep<MauiAddProjectPropertiesStep>();
            services.Services.AddTransient<DiagnosticAnalyzer, UsingXamarinFormsAnalyzerAnalyzer>();
            services.Services.AddTransient<DiagnosticAnalyzer, UsingXamarinEssentialsAnalyzer>();
            services.Services.AddTransient<CodeFixProvider, UsingXamarinFormsAnalyzerCodeFixProvider>();
            services.Services.AddTransient<CodeFixProvider, UsingXamarinEssentialsAnalyzerCodeFixProvider>();
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.LooseAssembly
{
    public class LooseDependencyAnalysisServiceProvider : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Services.AddTransient<IDependencyAnalyzer, LooseDependencyAnalyzer>();
            services.Services.AddScoped<NuGetPackageLookup>();

            services.AddExtensionOption<LooseDependencyOptions>("LooseDependencies");
        }
    }
}

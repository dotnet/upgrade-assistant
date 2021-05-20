﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.DotNet.UpgradeAssistant.Steps.Solution;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public static class AnalyzeExtensions
    {
        public static void AddAnalysis(this IServiceCollection services)
        {
            services.AddTransient<IAnalyzeResultProvider, AnalyzePackageStatus>();
            services.AddTransient<IDependencyAnalyzerRunner, DependencyAnalyzerRunner>();
            services.AddTransient<IEntrypointResolver, EntrypointResolver>();
        }
    }
}

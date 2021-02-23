// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Upgrader;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class UpgraderExtensions
    {
        public static void AddStepManagement(this IServiceCollection services)
        {
            services.AddScoped<UpgraderManager>();
            services.AddTransient<IUpgradeStepOrderer, UpgradeStepOrderer>();
        }
    }
}

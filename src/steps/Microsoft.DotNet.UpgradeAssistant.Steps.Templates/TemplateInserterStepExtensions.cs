// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Steps.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class TemplateInserterStepExtensions
    {
        public static IServiceCollection AddTemplateInserterStep(this IServiceCollection services)
        {
            services.AddSingleton<TemplateProvider>();
            services.AddScoped<UpgradeStep, TemplateInserterStep>();
            return services;
        }
    }
}

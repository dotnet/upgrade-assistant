// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Reporting;
using Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class ProjectFormatStepsExtensions
    {
        public static OptionsBuilder<TryConvertProjectConverterStepOptions> AddProjectFormatSteps(this IServiceCollection services)
        {
            services.AddScoped<MigrationStep, SetTFMStep>();
            services.AddScoped<MigrationStep, TryConvertProjectConverterStep>();
            services.AddSingleton<ITryConvertTool, TryConvertTool>();
            services.AddTransient<ISectionGenerator, TryConvertReport>();

            return services.AddOptions<TryConvertProjectConverterStepOptions>()
                .ValidateDataAnnotations();
        }
    }
}

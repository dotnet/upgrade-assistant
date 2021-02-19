// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    public static class TryConvertProjectConverterStepExtensions
    {
        public static OptionsBuilder<TryConvertProjectConverterStepOptions> AddTryConvertProjectConverterStep(this IServiceCollection services)
        {
            services.AddScoped<MigrationStep, SetTFMStep>();
            services.AddScoped<MigrationStep, TryConvertProjectConverterStep>();
            services.AddTransient<ISectionGenerator, TryConvertReport>();

            return services.AddOptions<TryConvertProjectConverterStepOptions>();
        }
    }
}

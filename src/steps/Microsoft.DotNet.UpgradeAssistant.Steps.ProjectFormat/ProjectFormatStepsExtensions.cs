﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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
            services.AddUpgradeStep<SetTFMStep>();
            services.AddUpgradeStep<TryConvertProjectConverterStep>();
            services.AddSingleton<ITryConvertTool, TryConvertTool>();
            services.AddTransient<ISectionGenerator, TryConvertReport>();

            return services.AddOptions<TryConvertProjectConverterStepOptions>()
                .PostConfigure(options =>
                {
                    if (!Path.IsPathRooted(options.TryConvertPath))
                    {
                        var directory = Path.GetDirectoryName(typeof(ProjectFormatStepsExtensions).Assembly.Location);
                        options.TryConvertPath = Path.GetFullPath(Path.Combine(directory, options.TryConvertPath));
                    }
                })
                .ValidateDataAnnotations();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class ProjectFormatStepsExtensions
    {
        public static OptionsBuilder<TryConvertProjectConverterStepOptions> AddProjectFormatSteps(this IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Services.AddUpgradeStep<SetTFMStep>();
            services.Services.AddUpgradeStep<TryConvertProjectConverterStep>();
            services.AddExtensionOption<TryConvertProjectConverterStepOptions>("TryConvertProjectConverter");
            services.Services.AddSingleton<ITryConvertTool, TryConvertTool>();

            return services.Services.AddOptions<TryConvertProjectConverterStepOptions>()
                .PostConfigure(options =>
                {
                    var path = Environment.ExpandEnvironmentVariables(options.TryConvertPath);

                    if (!Path.IsPathRooted(path))
                    {
                        var fileInfo = services.Files.GetFileInfo(options.TryConvertPath);

                        if (fileInfo.Exists && fileInfo.PhysicalPath is string physicalPath)
                        {
                            path = physicalPath;
                        }
                    }

                    options.TryConvertPath = path;
                })
                .ValidateDataAnnotations();
        }
    }
}

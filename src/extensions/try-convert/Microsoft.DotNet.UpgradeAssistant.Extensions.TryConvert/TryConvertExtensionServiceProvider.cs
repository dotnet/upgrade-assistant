// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.TryConvert
{
    public class TryConvertExtensionServiceProvider : IExtensionServiceProvider
    {
        private const string TryConvertProjectConverterStepOptionsSection = "TryConvert";

        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Services.AddUpgradeStep<SetTFMStep>();

            if (FeatureFlags.IsSolutionWideSdkConversionEnabled)
            {
                services.Services.AddUpgradeStep<SdkStyleConversionSolutionWideStep>();
            }
            else
            {
                services.Services.AddUpgradeStep<TryConvertProjectConverterStep>();
            }

            services.Services.AddTransient<ITryConvertTool, TryConvertInProcessTool>();
            services.Services.AddTransient<TryConvertRunner>();

            services.Services.AddOptions<TryConvertOptions>()
                .Bind(services.Configuration.GetSection(TryConvertProjectConverterStepOptionsSection))
                .PostConfigure(options =>
                {
                    var path = Environment.ExpandEnvironmentVariables(options.ToolPath);

                    if (!Path.IsPathRooted(path))
                    {
                        var fileInfo = services.Files.GetFileInfo(options.ToolPath);

                        if (fileInfo.Exists && fileInfo.PhysicalPath is string physicalPath)
                        {
                            path = physicalPath;
                        }
                    }

                    options.ToolPath = path;
                })
                .ValidateDataAnnotations();
        }
    }
}

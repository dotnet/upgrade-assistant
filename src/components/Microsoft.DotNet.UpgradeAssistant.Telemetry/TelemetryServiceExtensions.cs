// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TelemetryServiceExtensions
    {
        public static void AddTelemetry(this IServiceCollection services, Action<TelemetryOptions> configure)
        {
            services.AddOptions<TelemetryOptions>()
                .PostConfigure(options =>
                {
                    if (string.IsNullOrEmpty(options.CurrentSessionId))
                    {
                        options.CurrentSessionId = Guid.NewGuid().ToString();
                    }
                })
                .Configure(configure);

            services.AddSingleton<IMacAddressProvider, MacAddressGetter>();
            services.AddSingleton<IFileManager, FileManager>();
            services.AddSingleton<IDockerContainerDetector, DockerContainerDetectorForTelemetry>();
            services.AddSingleton<IUserLevelCacheWriter, UserLevelCacheWriter>();
            services.AddSingleton<ITelemetry, Telemetry>();
            services.AddSingleton<IStringHasher, Sha256Hasher>();
            services.AddSingleton<IFirstTimeUseNoticeSentinel, FirstTimeUseNoticeSentinel>();

            services.AddSingleton<TelemetryCommonProperties>();
            services.AddTransient<ITelemetryInitializer>(ctx => ctx.GetRequiredService<TelemetryCommonProperties>());
        }
    }
}

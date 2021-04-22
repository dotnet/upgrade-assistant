// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.DotNet.UpgradeAssistant.Cli;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Integration.Tests
{
    public class UpgradeRunner
    {
        public UnknownPackages UnknownPackages { get; } = new UnknownPackages();

        public async Task<int> UpgradeAsync(string inputPath, string entrypoint, ITestOutputHelper output, TimeSpan maxDuration)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentException($"'{nameof(inputPath)}' cannot be null or empty.", nameof(inputPath));
            }

            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            var project = new FileInfo(inputPath);
            using var cts = new CancellationTokenSource(maxDuration);

            var options = new UpgradeOptions
            {
                SkipBackup = true,
                Project = project,
                NonInteractive = true,
                NonInteractiveWait = 0,
                EntryPoint = new[] { entrypoint },
            };

            return await Program.RunUpgradeAsync(options, host => host
                .ConfigureServices((_, services) =>
                {
                    services.AddOptions<PackageUpdaterOptions>().Configure(o =>
                    {
                        o.PackageMapPath = "PackageMaps";
                    });
                })
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    builder.RegisterType<KnownPackages>()
                        .SingleInstance()
                        .AsSelf();

                    builder.RegisterInstance(UnknownPackages);
                    builder.RegisterDecorator<InterceptingKnownPackageLoader, IPackageLoader>();
                })
                .ConfigureLogging((ctx, logging) =>
                {
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddProvider(new TestOutputHelperLoggerProvider(output));
                }),
                cts.Token).ConfigureAwait(false);
        }
    }
}

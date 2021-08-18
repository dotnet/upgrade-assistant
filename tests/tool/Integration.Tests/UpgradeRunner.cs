// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.DotNet.UpgradeAssistant.Cli;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
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

            var options = new TestOptions(project);
            var status = await Host.CreateDefaultBuilder()
                .UseEnvironment(Environments.Development)
                .UseUpgradeAssistant<ConsoleUpgrade>(options)
                .ConfigureServices(services =>
                {
                    services.AddNonInteractive(options => options.Wait = TimeSpan.Zero, true);
                    services.AddKnownExtensionOptions(new() { Entrypoints = new[] { entrypoint }, SkipBackup = true });
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
                })
                .RunUpgradeAssistantAsync(cts.Token).ConfigureAwait(false);

            if (cts.Token.IsCancellationRequested)
            {
                throw new TimeoutException("The integration test could not complete successfully");
            }

            return status;
        }

        private record TestOptions(FileInfo Project) : IUpgradeAssistantOptions
        {
            public bool IsVerbose => true;

            public bool IgnoreUnsupportedFeatures => false;

            public UpgradeTarget TargetTfmSupport => UpgradeTarget.Current;

            public IReadOnlyCollection<string> Extension => Array.Empty<string>();

            public IEnumerable<AdditionalOption> AdditionalOptions => Enumerable.Empty<AdditionalOption>();

            public DirectoryInfo? VSPath { get; }

            public DirectoryInfo? MSBuildPath { get; }
        }
    }
}

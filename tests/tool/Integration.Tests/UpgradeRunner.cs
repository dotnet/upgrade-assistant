// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.DotNet.UpgradeAssistant.Cli;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests
{
    public static class UpgradeRunner
    {
        public static async Task UpgradeAsync(string inputPath, TextWriter output, int timeoutSeconds = 300)
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
            using var cts = new CancellationTokenSource();

            var options = new UpgradeOptions
            {
                SkipBackup = true,
                Project = project,
                NonInteractive = true,
                NonInteractiveWait = 0,
            };

            var upgradeTask = Program.RunUpgradeAsync(options, (context, services) => RegisterTestServices(services), cts.Token);
            var timeoutTimer = Task.Delay(timeoutSeconds * 1000, cts.Token);

            await Task.WhenAny(upgradeTask, timeoutTimer).ConfigureAwait(false);
            cts.Cancel();

            try
            {
                await Task.WhenAll(upgradeTask, timeoutTimer).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static void RegisterTestServices(IServiceCollection services)
        {
            services.AddOptions<PackageUpdaterOptions>().Configure(o =>
            {
                o.PackageMapPath = "PackageMaps";
                o.UpgradeAnalyzersPackageVersion = "1.0.0";
            });
        }
    }
}

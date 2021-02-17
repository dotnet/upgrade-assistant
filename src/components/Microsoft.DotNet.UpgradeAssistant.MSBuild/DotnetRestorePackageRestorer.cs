﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using static System.FormattableString;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class DotnetRestorePackageRestorer : IPackageRestorer
    {
        private static readonly string[] EnvVarsToWitholdFromTryConvert = new string[] { "MSBuildSDKsPath", "MSBuildExtensionsPath", "MSBUILD_EXE_PATH" };

        private readonly ILogger<DotnetRestorePackageRestorer> _logger;

        public DotnetRestorePackageRestorer(ILogger<DotnetRestorePackageRestorer> logger)
        {
            _logger = logger;
        }

        public async Task RestoreAllProjectPackagesAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _logger.LogInformation("Restoring all projects");

            await RunRestoreAsync(context, context.EntryPoint.Required().FilePath, token);

            // Reload the project because, by design, NuGet properties (like NuGetPackageRoot)
            // aren't available in a project until after restore is run the first time.
            // https://github.com/NuGet/Home/issues/9150
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);
        }

        public async Task<RestoreOutput> RestorePackagesAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await RunRestoreAsync(context, context.CurrentProject.Required().FilePath, token);

            // Reload the project because, by design, NuGet properties (like NuGetPackageRoot)
            // aren't available in a project until after restore is run the first time.
            // https://github.com/NuGet/Home/issues/9150
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);

            return GetRestoreOutput(context.CurrentProject.Required());
        }

        private RestoreOutput GetRestoreOutput(IProject project)
        {
            // Check for the lock file's existence rather than success since a bad NuGet reference won't
            // prevent other (valid) packages from being restored and we may still have a (partial) lock file.
            var lockFilePath = project.LockFilePath;

            // Get the path used for caching NuGet packages
            var nugetCachePath = project.GetFile().GetPropertyValue("NuGetPackageRoot");

            return new RestoreOutput(lockFilePath, Directory.Exists(nugetCachePath) ? nugetCachePath : null);
        }

        private async Task<bool> RunRestoreAsync(IMigrationContext context, string path, CancellationToken token)
        {
            using var restoreProcess = new Process
            {
                StartInfo = new ProcessStartInfo("dotnet", Invariant($"restore {path}"))
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            foreach (var (name, value) in context.GlobalProperties)
            {
                restoreProcess.StartInfo.EnvironmentVariables[name] = value;
            }

            foreach (var envVar in EnvVarsToWitholdFromTryConvert)
            {
                if (restoreProcess.StartInfo.EnvironmentVariables.ContainsKey(envVar))
                {
                    restoreProcess.StartInfo.EnvironmentVariables.Remove(envVar);
                }
            }

            restoreProcess.OutputDataReceived += TryConvertOutputReceived;
            restoreProcess.ErrorDataReceived += TryConvertErrorReceived;
            restoreProcess.Start();
            restoreProcess.BeginOutputReadLine();
            restoreProcess.BeginErrorReadLine();

            await restoreProcess.WaitForExitAsync(token).ConfigureAwait(false);

            return restoreProcess.ExitCode == 0;

            void TryConvertOutputReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _logger.LogInformation($"[dotnet-restore] {e.Data}");
                }
            }

            void TryConvertErrorReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _logger.LogError($"[dotnet-restore] {e.Data}");
                }
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Exceptions;

namespace AspNetMigrator.Engine
{
    public class TryConvertProjectConverterStep : MigrationStep
    {
        private const string DefaultSDK = "Microsoft.NET.Sdk";
        private const string TryConvertArgumentsFormat = "--no-backup --force-web-conversion -p {0}";
        private static readonly string[] EnvVarsToWitholdFromTryConvert = new string[] { "MSBuildSDKsPath", "MSBuildExtensionsPath", "MSBUILD_EXE_PATH" };
        private static readonly string TryConvertPath =
            Path.Combine(Path.GetDirectoryName(typeof(TryConvertProjectConverterStep).Assembly.Location), "tools", "try-convert.exe");

        public TryConvertProjectConverterStep(MigrateOptions options, ILogger logger)
            : base(options, logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Title = $"Convert project file to SDK style";
            Description = $"Convert {options.ProjectPath} to an SDK-style project with try-convert";
        }

        protected async override Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (!File.Exists(Options.ProjectPath))
            {
                Logger.Fatal("Project file {ProjectPath} not found", Options.ProjectPath);
                return (MigrationStepStatus.Failed, $"Project file {Options.ProjectPath} not found");
            }

            Logger.Information("Converting project file format with try-convert");
            using var tryConvertProcess = new Process
            {
                StartInfo = new ProcessStartInfo(TryConvertPath, string.Format(CultureInfo.InvariantCulture, TryConvertArgumentsFormat, Options.ProjectPath))
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            // Clear some MSBuild env vars that can prevent try-convert from successfully
            // opening non-SDK projects.
            foreach (var envVar in EnvVarsToWitholdFromTryConvert)
            {
                if (tryConvertProcess.StartInfo.EnvironmentVariables.ContainsKey(envVar))
                {
                    tryConvertProcess.StartInfo.EnvironmentVariables.Remove(envVar);
                }
            }

            tryConvertProcess.OutputDataReceived += TryConvertOutputReceived;
            tryConvertProcess.ErrorDataReceived += TryConvertErrorReceived;
            tryConvertProcess.Start();
            tryConvertProcess.BeginOutputReadLine();
            tryConvertProcess.BeginErrorReadLine();
            await tryConvertProcess.WaitForExitAsync(token).ConfigureAwait(false);

            if (tryConvertProcess.ExitCode != 0)
            {
                Logger.Fatal("Conversion with try-convert failed (exit code {ExitCode})", tryConvertProcess.ExitCode);
                return (MigrationStepStatus.Failed, $"Convesion with try-convert failed (exit code {tryConvertProcess.ExitCode})");
            }
            else
            {
                Logger.Information("Project file format conversion successful");
                return (MigrationStepStatus.Complete, "Project file converted successfully");
            }
        }

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            try
            {
                var project = ProjectRootElement.Open(Options.ProjectPath);
                project.Reload(false); // Reload to make sure we're not seeing an old cached version of the project

                // SDK-style projects should reference the Microsoft.NET.Sdk SDK
                if (project.Sdk is null || !project.Sdk.Contains(DefaultSDK, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Verbose("Project {ProjectPath} not yet converted", Options.ProjectPath);
                    return Task.FromResult<(MigrationStepStatus, string)>((MigrationStepStatus.Incomplete,
                        $"Project {Options.ProjectPath} is not an SDK project. Applying this step will execute the following try-convert command line: {TryConvertPath} {string.Format(CultureInfo.InvariantCulture, TryConvertArgumentsFormat, Options.ProjectPath)}"));
                }
                else
                {
                    Logger.Verbose("Project {ProjectPath} already targets SDK {SDK}", Options.ProjectPath, project.Sdk);
                    return Task.FromResult((MigrationStepStatus.Complete, $"Project already targets {project.Sdk} SDK"));
                }
            }
            catch (InvalidProjectFileException exc)
            {
                Logger.Error("Failed to open project {ProjectPath}; Exception: {Exception}", Options.ProjectPath, exc.ToString());
                return Task.FromResult((MigrationStepStatus.Failed, $"Failed to open project {Options.ProjectPath}"));
            }
        }

        private void TryConvertOutputReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Logger.Information($"[try-convert] {e.Data}");
            }
        }

        private void TryConvertErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Logger.Error($"[try-convert] {e.Data}");
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetMigrator.TryConvertUpdater
{
    public class TryConvertProjectConverterStep : MigrationStep
    {
        private const string DefaultSDK = "Microsoft.NET.Sdk";
        private const string TryConvertArgumentsFormat = "--no-backup --force-web-conversion -p \"{0}\"";
        private static readonly string[] EnvVarsToWitholdFromTryConvert = new string[] { "MSBuildSDKsPath", "MSBuildExtensionsPath", "MSBUILD_EXE_PATH" };
        private static readonly string[] ErrorMessages = new[] { "This project has custom imports that are not accepted by try-convert" };

        private readonly string _tryConvertPath;
        private bool _errorEncountered;

        public TryConvertProjectConverterStep(MigrateOptions migrateOptions, IOptions<TryConvertProjectConverterStepOptions> tryConvertOptionsAccessor, ILogger<TryConvertProjectConverterStep> logger)
            : base(migrateOptions, logger)
        {
            if (migrateOptions is null)
            {
                throw new ArgumentNullException(nameof(migrateOptions));
            }

            if (tryConvertOptionsAccessor is null)
            {
                throw new ArgumentNullException(nameof(tryConvertOptionsAccessor));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Title = $"Convert project file to SDK style";
            Description = $"Convert {migrateOptions.ProjectPath} to an SDK-style project with try-convert";
            var rawPath = tryConvertOptionsAccessor.Value?.TryConvertPath ?? throw new ArgumentException("Try-Convert options must be provided with a non-null TryConvertPath");
            _tryConvertPath = Environment.ExpandEnvironmentVariables(rawPath);
        }

        protected async override Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = await context.GetProjectAsync(token).ConfigureAwait(false);
            var projectPath = project?.FilePath;

            if (!File.Exists(projectPath))
            {
                Logger.LogCritical("Project file {ProjectPath} not found", projectPath);
                return (MigrationStepStatus.Failed, $"Project file {projectPath} not found");
            }

            Logger.LogInformation("Converting project file format with try-convert");
            using var tryConvertProcess = new Process
            {
                StartInfo = new ProcessStartInfo(_tryConvertPath, string.Format(CultureInfo.InvariantCulture, TryConvertArgumentsFormat, projectPath))
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            await foreach (var (name, value) in context.GetWorkspaceProperties(token))
            {
                tryConvertProcess.StartInfo.EnvironmentVariables[name] = value;
            }

            // Clear some MSBuild env vars that can prevent try-convert from successfully
            // opening non-SDK projects.
            foreach (var envVar in EnvVarsToWitholdFromTryConvert)
            {
                if (tryConvertProcess.StartInfo.EnvironmentVariables.ContainsKey(envVar))
                {
                    tryConvertProcess.StartInfo.EnvironmentVariables.Remove(envVar);
                }
            }

            _errorEncountered = false;
            tryConvertProcess.OutputDataReceived += TryConvertOutputReceived;
            tryConvertProcess.ErrorDataReceived += TryConvertErrorReceived;
            tryConvertProcess.Start();
            tryConvertProcess.BeginOutputReadLine();
            tryConvertProcess.BeginErrorReadLine();
            await tryConvertProcess.WaitForExitAsync(token).ConfigureAwait(false);

            if (tryConvertProcess.ExitCode != 0 || _errorEncountered)
            {
                Logger.LogCritical("Conversion with try-convert failed (exit code {ExitCode}). Make sure Try-Convert (version 0.7.157502 or higher) is installed and that your project does not use custom imports.", tryConvertProcess.ExitCode);
                return (MigrationStepStatus.Failed, $"Conversion with try-convert failed (exit code {tryConvertProcess.ExitCode})");
            }
            else
            {
                Logger.LogInformation("Project file format conversion successful");
                return (MigrationStepStatus.Complete, "Project file converted successfully");
            }
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectPath = await context.GetProjectPathAsync(token).ConfigureAwait(false);

            if (projectPath is null)
            {
                Logger.LogCritical("No project path specified");
                return (MigrationStepStatus.Failed, "No project specified");
            }

            if (!File.Exists(_tryConvertPath))
            {
                Logger.LogCritical("Try-Convert not found");
                return (MigrationStepStatus.Failed, "Try-Convert not found. This tool depends on the Try-Convert CLI tool. Please ensure that Try-Convert is installed and that the correct location for the tool is specified (in configuration, for example). https://github.com/dotnet/try-convert");
            }

            try
            {
                var project = await context.GetProjectRootElementAsync(token).ConfigureAwait(false);

                // SDK-style projects should reference the Microsoft.NET.Sdk SDK
                if (project.Sdk is null || !project.Sdk.Contains(DefaultSDK, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogDebug("Project {ProjectPath} not yet converted", projectPath);
                    return (MigrationStepStatus.Incomplete, $"Project {projectPath} is not an SDK project. Applying this step will execute the following try-convert command line: {_tryConvertPath} {string.Format(CultureInfo.InvariantCulture, TryConvertArgumentsFormat, projectPath)}");
                }
                else
                {
                    Logger.LogDebug("Project {ProjectPath} already targets SDK {SDK}", projectPath, project.Sdk);
                    return (MigrationStepStatus.Complete, $"Project already targets {project.Sdk} SDK");
                }
            }
            catch (InvalidProjectFileException exc)
            {
                Logger.LogError("Failed to open project {ProjectPath}; Exception: {Exception}", projectPath, exc.ToString());
                return (MigrationStepStatus.Failed, $"Failed to open project {projectPath}");
            }
        }

        private void TryConvertOutputReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                CheckForErrors(e.Data);
                Logger.LogInformation($"[try-convert] {e.Data}");
            }
        }

        private void TryConvertErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                CheckForErrors(e.Data);
                Logger.LogError($"[try-convert] {e.Data}");
            }
        }

        private void CheckForErrors(string data)
        {
            if (ErrorMessages.Any(data.Contains))
            {
                _errorEncountered = true;
            }
        }
    }
}

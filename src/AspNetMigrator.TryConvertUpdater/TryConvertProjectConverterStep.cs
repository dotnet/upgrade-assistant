using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetMigrator.TryConvertUpdater
{
    public class TryConvertProjectConverterStep : MigrationStep
    {
        private const string TryConvertArgumentsFormat = "--no-backup --force-web-conversion -p \"{0}\"";
        private static readonly string[] EnvVarsToWitholdFromTryConvert = new string[] { "MSBuildSDKsPath", "MSBuildExtensionsPath", "MSBUILD_EXE_PATH" };
        private static readonly string[] ErrorMessages = new[] { "This project has custom imports that are not accepted by try-convert" };

        private readonly string _tryConvertPath;

        public override string Id => typeof(TryConvertProjectConverterStep).FullName!;

        public override string Description => $"Use the try-convert tool ({_tryConvertPath}) to convert the project file to an SDK-style csproj";

        public override string Title => $"Convert project file to SDK style";

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing package references
            "AspNetMigrator.BackupUpdater.BackupStep"
        };

        public TryConvertProjectConverterStep(IOptions<TryConvertProjectConverterStepOptions> tryConvertOptionsAccessor, ILogger<TryConvertProjectConverterStep> logger)
            : base(logger)
        {
            if (tryConvertOptionsAccessor is null)
            {
                throw new ArgumentNullException(nameof(tryConvertOptionsAccessor));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var rawPath = tryConvertOptionsAccessor.Value?.TryConvertPath ?? throw new ArgumentException("Try-Convert options must be provided with a non-null TryConvertPath. App configuration may be missing or invalid.");
            _tryConvertPath = Environment.ExpandEnvironmentVariables(rawPath);
        }

        protected override bool IsApplicableImpl(IMigrationContext context) => context?.CurrentProject is not null;

        protected async override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectPath = context.CurrentProject.Required().Project.FilePath;

            if (!File.Exists(projectPath))
            {
                Logger.LogCritical("Project file {ProjectPath} not found", projectPath);
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Project file {projectPath} not found");
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

            var errorEncountered = false;

            tryConvertProcess.OutputDataReceived += TryConvertOutputReceived;
            tryConvertProcess.ErrorDataReceived += TryConvertErrorReceived;
            tryConvertProcess.Start();
            tryConvertProcess.BeginOutputReadLine();
            tryConvertProcess.BeginErrorReadLine();

            await tryConvertProcess.WaitForExitAsync(token).ConfigureAwait(false);

            // Reload the workspace since an external process worked on and
            // may have changed the workspace's project.
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);

            if (tryConvertProcess.ExitCode != 0 || errorEncountered)
            {
                Logger.LogCritical("Conversion with try-convert failed (exit code {ExitCode}). Make sure Try-Convert (version 0.7.157502 or higher) is installed and that your project does not use custom imports.", tryConvertProcess.ExitCode);
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Conversion with try-convert failed (exit code {tryConvertProcess.ExitCode})");
            }
            else
            {
                Logger.LogInformation("Project file converted successfully! The project may require additional changes to build successfully against the new .NET target.");
                return new MigrationStepApplyResult(MigrationStepStatus.Complete, "Project file converted successfully");
            }

            void TryConvertOutputReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    CheckForErrors(e.Data);
                    Logger.LogInformation($"[try-convert] {e.Data}");
                }
            }

            void TryConvertErrorReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    CheckForErrors(e.Data);
                    Logger.LogError($"[try-convert] {e.Data}");
                }
            }

            void CheckForErrors(string data)
            {
                if (ErrorMessages.Any(data.Contains))
                {
                    errorEncountered = true;
                }
            }
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
            => Task.FromResult(InitializeImpl(context));

        private MigrationStepInitializeResult InitializeImpl(IMigrationContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required().Project;

            if (!File.Exists(_tryConvertPath))
            {
                Logger.LogCritical("Try-Convert not found. This tool depends on the Try-Convert CLI tool. Please ensure that Try-Convert is installed and that the correct location for the tool is specified (in configuration, for example). https://github.com/dotnet/try-convert");
                return new MigrationStepInitializeResult(MigrationStepStatus.Failed, "Try-Convert not found. This tool depends on the Try-Convert CLI tool. Please ensure that Try-Convert is installed and that the correct location for the tool is specified (in configuration, for example). https://github.com/dotnet/try-convert", BuildBreakRisk.Unknown);
            }

            var projectFile = project.GetFile();

            try
            {
                // SDK-style projects should reference the Microsoft.NET.Sdk SDK
                if (!projectFile.IsSdk)
                {
                    Logger.LogDebug("Project {ProjectPath} not yet converted", projectFile.FilePath);
                    return new MigrationStepInitializeResult(
                        MigrationStepStatus.Incomplete,
                        $"Project {projectFile.FilePath} is not an SDK project. Applying this step will execute the following try-convert command line to convert the project to an SDK-style project and retarget it to .NET Core/Standard: {_tryConvertPath} {string.Format(CultureInfo.InvariantCulture, TryConvertArgumentsFormat, projectFile.FilePath)}",
                        project.Style == ProjectStyle.Web ? BuildBreakRisk.High : BuildBreakRisk.Medium);
                }
                else
                {
                    Logger.LogDebug("Project {ProjectPath} already targets SDK {SDK}", projectFile.FilePath, projectFile.Sdk);
                    return new MigrationStepInitializeResult(MigrationStepStatus.Complete, $"Project already targets {projectFile.Sdk} SDK", BuildBreakRisk.None);
                }
            }
            catch (Exception exc)
            {
                Logger.LogError("Failed to open project {ProjectPath}; Exception: {Exception}", projectFile.FilePath, exc.ToString());
                return new MigrationStepInitializeResult(MigrationStepStatus.Failed, $"Failed to open project {projectFile.FilePath}", BuildBreakRisk.Unknown);
            }
        }
      }
}

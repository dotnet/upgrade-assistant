// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat
{
    public class TryConvertProjectConverterStep : UpgradeStep
    {
        private readonly ITryConvertTool _runner;
        private readonly IPackageRestorer _restorer;

        public override string Description => $"Use the try-convert tool ({_runner.Path}) to convert the project file to an SDK-style csproj";

        public override string Title => $"Convert project file to SDK style";

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing package references
            "Microsoft.DotNet.UpgradeAssistant.Steps.Backup.BackupStep"
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.NextProjectStep",
        };

        public TryConvertProjectConverterStep(
            ITryConvertTool runner,
            IPackageRestorer restorer,
            ILogger<TryConvertProjectConverterStep> logger)
            : base(logger)
        {
            _restorer = restorer ?? throw new ArgumentNullException(nameof(restorer));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        protected override bool IsApplicableImpl(IUpgradeContext context) => context?.CurrentProject is not null
            /* try convert does not support the migration of Visual Basic WPF applications */
            && !(context.CurrentProject.Language == Languages.VisualBasic && (context.CurrentProject.Components & ProjectComponents.WPF) == ProjectComponents.WPF);

        protected async override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var result = await RunTryConvertAsync(context, context.CurrentProject.Required(), token).ConfigureAwait(false);

            await _restorer.RestorePackagesAsync(context, context.CurrentProject.Required(), token).ConfigureAwait(false);

            return result;
        }

        private async Task<UpgradeStepApplyResult> RunTryConvertAsync(IUpgradeContext context, IProject project, CancellationToken token)
        {
            Logger.LogInformation("Converting project file format with try-convert");

            var result = await _runner.RunAsync(context, project, token).ConfigureAwait(false);

            // Reload the workspace since an external process worked on and
            // may have changed the workspace's project.
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);

            if (!result)
            {
                Logger.LogCritical("Conversion with try-convert failed. Make sure Try-Convert (version 0.7.157502 or higher) is installed and that your project does not use custom imports.");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Conversion with try-convert failed.");
            }
            else
            {
                Logger.LogInformation("Project file converted successfully! The project may require additional changes to build successfully against the new .NET target.");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Project file converted successfully");
            }
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
            => Task.FromResult(InitializeImpl(context));

        private UpgradeStepInitializeResult InitializeImpl(IUpgradeContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            if (!_runner.IsAvailable)
            {
                Logger.LogCritical("Try-Convert not found. This tool depends on the Try-Convert CLI tool. Please ensure that Try-Convert is installed and that the correct location for the tool is specified (in configuration, for example). https://github.com/dotnet/try-convert");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, "Try-Convert not found. This tool depends on the Try-Convert CLI tool. Please ensure that Try-Convert is installed and that the correct location for the tool is specified (in configuration, for example). https://github.com/dotnet/try-convert", BuildBreakRisk.Unknown);
            }

            var projectFile = project.GetFile();

            try
            {
                // SDK-style projects should reference the Microsoft.NET.Sdk SDK
                if (!projectFile.IsSdk)
                {
                    Logger.LogDebug("Project {ProjectPath} not yet converted", projectFile.FilePath);
                    return new UpgradeStepInitializeResult(
                        UpgradeStepStatus.Incomplete,
                        $"Project {projectFile.FilePath} is not an SDK project. Applying this step will execute the following try-convert command line to convert the project to an SDK-style project: {_runner.GetCommandLine(project)}",
                        (project.Components & ProjectComponents.Web) == ProjectComponents.Web ? BuildBreakRisk.High : BuildBreakRisk.Medium);
                }
                else
                {
                    Logger.LogDebug("Project {ProjectPath} already targets SDK {SDK}", projectFile.FilePath, projectFile.Sdk);
                    return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, $"Project already targets {projectFile.Sdk} SDK", BuildBreakRisk.None);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogError("Failed to open project {ProjectPath}; Exception: {Exception}", projectFile.FilePath, exc.ToString());
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Failed to open project {projectFile.FilePath}", BuildBreakRisk.Unknown);
            }
        }
    }
}

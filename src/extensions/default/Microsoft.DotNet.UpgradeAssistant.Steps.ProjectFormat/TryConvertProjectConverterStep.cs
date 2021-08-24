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

        private string VersionString => _runner?.Version is null
            ? string.Empty
            : $", version {_runner.Version}";

        public override string Description => $"Use the try-convert tool ({_runner.Path}{VersionString}) to convert the project file to an SDK-style csproj";

        public override string Title => $"Convert project file to SDK style";

        public override string Id => WellKnownStepIds.TryConvertProjectConverterStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing package references
            WellKnownStepIds.BackupStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
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

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is not null);

        protected async override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();
            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (components.HasFlag(ProjectComponents.XamarinAndroid) || components.HasFlag(ProjectComponents.XamariniOS))
            {
                context.Properties.SetPropertyValue("componentFlag", components.ToString(), true);
            }

            var result = await RunTryConvertAsync(context, context.CurrentProject.Required(), token).ConfigureAwait(false);

            await _restorer.RestorePackagesAsync(context, context.CurrentProject.Required(), token).ConfigureAwait(false);

            return result;
        }

        private async Task<UpgradeStepApplyResult> RunTryConvertAsync(IUpgradeContext context, IProject project, CancellationToken token)
        {
            Logger.LogInformation($"Converting project file format with try-convert{VersionString}");

            var result = await _runner.RunAsync(context, project, token).ConfigureAwait(false);

            // Reload the workspace since an external process worked on and
            // may have changed the workspace's project.
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);

            if (!result)
            {
                Logger.LogCritical("Conversion with try-convert failed.");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Conversion with try-convert failed.");
            }
            else
            {
                Logger.LogInformation("Project file converted successfully! The project may require additional changes to build successfully against the new .NET target.");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Project file converted successfully");
            }
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            if (!_runner.IsAvailable)
            {
                throw new UpgradeException($"try-convert not found: {_runner.Path}");
            }

            var projectFile = project.GetFile();

            try
            {
                // SDK-style projects should reference the Microsoft.NET.Sdk SDK
                if (!projectFile.IsSdk)
                {
                    Logger.LogDebug("Project {ProjectPath} not yet converted", projectFile.FilePath);

                    var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

                    return new UpgradeStepInitializeResult(
                        UpgradeStepStatus.Incomplete,
                        $"Project {projectFile.FilePath} is not an SDK project. Applying this step will convert the project to SDK style.",
                        components.HasFlag(ProjectComponents.AspNetCore) ? BuildBreakRisk.High : BuildBreakRisk.Medium);
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
                Logger.LogError(exc, "Failed to open project {ProjectPath}", projectFile.FilePath);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Failed to open project {projectFile.FilePath}", BuildBreakRisk.Unknown);
            }
        }
    }
}

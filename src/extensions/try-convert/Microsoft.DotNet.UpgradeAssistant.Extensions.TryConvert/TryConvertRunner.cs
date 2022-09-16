// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.TryConvert
{
    public class TryConvertRunner
    {
        private readonly ITryConvertTool _tool;
        private readonly IPackageRestorer _restorer;
        private readonly ILogger _logger;

        public TryConvertRunner(ITryConvertTool tool, IPackageRestorer restorer, ILogger<TryConvertRunner> logger)
        {
            _tool = tool;
            _restorer = restorer;
            _logger = logger;
        }

        public string VersionString => _tool?.Version is null ? string.Empty : $", version {_tool.Version}";

        public string Path => _tool.Path;

        public async Task<UpgradeStepApplyResult> ApplyAsync(IUpgradeContext context, IProject project, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);
            if (components.HasFlag(ProjectComponents.XamarinAndroid) || components.HasFlag(ProjectComponents.XamariniOS))
            {
                context.Properties.SetPropertyValue("componentFlag", components.ToString(), true);
            }

            var result = await RunTryConvertAsync(context, project, token).ConfigureAwait(false);

            await _restorer.RestorePackagesAsync(context, project, token).ConfigureAwait(false);

            return result;
        }

        public async Task<UpgradeStepInitializeResult> InitializeAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (!_tool.IsAvailable)
            {
                throw new UpgradeException($"try-convert not found: {_tool.Path}");
            }

            var projectFile = project.GetFile();

            // SDK-style projects should reference the Microsoft.NET.Sdk SDK
            if (!projectFile.IsSdk)
            {
                _logger.LogDebug("Project {ProjectPath} not yet converted", projectFile.FilePath);

                var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

                return new UpgradeStepInitializeResult(
                    UpgradeStepStatus.Incomplete,
                    $"Project {projectFile.FilePath} is not an SDK project. Applying this step will convert the project to SDK style.",
                    components.HasFlag(ProjectComponents.AspNetCore) ? BuildBreakRisk.High : BuildBreakRisk.Medium);
            }
            else
            {
                _logger.LogDebug("Project {ProjectPath} already targets SDK {SDK}", projectFile.FilePath, projectFile.Sdk);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, $"Project already targets {projectFile.Sdk} SDK", BuildBreakRisk.None);
            }
        }

        private void AddResultToContext(IUpgradeContext context, UpgradeStepStatus status, string resultMessage)
        {
            context.AddResult(TryConvertProjectConverterStep.StepTitle, context.CurrentProject?.GetFile()?.FilePath ?? string.Empty,
                WellKnownStepIds.TryConvertProjectConverterStepId, status, resultMessage);
        }

        private async Task<UpgradeStepApplyResult> RunTryConvertAsync(IUpgradeContext context, IProject project, CancellationToken token)
        {
            _logger.LogInformation($"Converting project file format with try-convert{VersionString}");

            var result = await _tool.RunAsync(context, project, token).ConfigureAwait(false);

            // Reload the workspace since an external process worked on and
            // may have changed the workspace's project.
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);

            if (!result)
            {
                var description = "Conversion with try-convert failed.";
                _logger.LogCritical(description);
                AddResultToContext(context, UpgradeStepStatus.Failed, description);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, description);
            }
            else
            {
                var description = "Project file converted successfully! The project may require additional changes to build successfully against the new .NET target.";
                _logger.LogInformation(description);
                AddResultToContext(context, UpgradeStepStatus.Complete, description);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Project file converted successfully");
            }
        }
    }
}

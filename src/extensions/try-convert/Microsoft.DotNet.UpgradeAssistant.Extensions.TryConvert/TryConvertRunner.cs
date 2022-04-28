// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

            await _restorer.RestorePackagesAsync(context, project, token).ConfigureAwait(false);

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

        private async Task<UpgradeStepApplyResult> RunTryConvertAsync(IUpgradeContext context, IProject project, CancellationToken token)
        {
            _logger.LogInformation($"Converting project file format with try-convert{VersionString}");

            var result = await _tool.RunAsync(context, project, token).ConfigureAwait(false);

            // Reload the workspace since an external process worked on and
            // may have changed the workspace's project.
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);

            if (!result)
            {
                _logger.LogCritical("Conversion with try-convert failed.");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Conversion with try-convert failed.");
            }
            else
            {
                _logger.LogInformation("Project file converted successfully! The project may require additional changes to build successfully against the new .NET target.");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Project file converted successfully");
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSBuild.Abstractions;
using MSBuild.Conversion.Project;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.TryConvert
{
    public class TryConvertInProcessTool : ITryConvertTool
    {
        private readonly ILogger<TryConvertInProcessTool> _logger;
        private readonly ITargetFrameworkSelector _targetFrameworkSelector;

        public TryConvertInProcessTool(ILogger<TryConvertInProcessTool> logger, ITargetFrameworkSelector targetFrameworkSelector)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _targetFrameworkSelector = targetFrameworkSelector ?? throw new ArgumentNullException(nameof(targetFrameworkSelector));
        }

        public bool IsAvailable => true;

        public string Path { get; } = System.IO.Path.GetDirectoryName(typeof(MSBuildConversionWorkspace).Assembly.Location)!;

        public string? Version { get; } = typeof(MSBuildConversionWorkspace).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        public async Task<bool> RunAsync(IUpgradeContext context, IProject project, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var tfm = await _targetFrameworkSelector.SelectTargetFrameworkAsync(project, token).ConfigureAwait(false);

            // try-convert has no overloads to pass in properties
            SetEnvironmentVariables(context);

            var workspaceLoader = new MSBuildConversionWorkspaceLoader(project.FileInfo.FullName, MSBuildConversionWorkspaceType.Project);
            var msbuildWorkspace = workspaceLoader.LoadWorkspace(project.FileInfo.FullName, noBackup: true, tfm.ToString(), keepCurrentTFMs: true, forceWeb: true);

            if (msbuildWorkspace.WorkspaceItems.Length is 0)
            {
                _logger.LogWarning("No projects were converted to SDK style");
                return false;
            }

            foreach (var item in msbuildWorkspace.WorkspaceItems)
            {
                _logger.LogInformation("Converting project {Path} to SDK style", item.ProjectRootElement.FullPath);
                var converter = new Converter(item.UnconfiguredProject, item.SdkBaselineProject, item.ProjectRootElement, noBackup: true, forceRemoveCustomImports: true);
                converter.Convert(item.ProjectRootElement.FullPath);
            }

            return true;
        }

        private static void SetEnvironmentVariables(IUpgradeContext context)
        {
            foreach (var (key, value) in context.GlobalProperties)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}

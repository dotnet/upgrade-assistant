// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    /// <summary>
    /// Adds the System.Configuration.ConfigurationManager package when needed.
    /// Scenarios include:
    ///  1. VB Class Libraries that have a reference to the 'My.' namespace.
    /// </summary>
    public class SystemConfigurationAnalyzer : IDependencyAnalyzer
    {
        private const string SystemConfigurationPackageName = "System.Configuration.ConfigurationManager";

        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<SystemConfigurationAnalyzer> _logger;

        public string Name => nameof(SystemConfigurationAnalyzer) + " reference analyzer";

        public SystemConfigurationAnalyzer(IPackageLoader packageLoader, ILogger<SystemConfigurationAnalyzer> logger, ITargetFrameworkMonikerComparer tfmComparer)
        {
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AnalyzeAsync(IProject project, IDependencyAnalysisState state, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!IsVbClassLibrary(project) || !project.TargetFrameworks.Any(tfm => tfm.IsNetStandard))
            {
                // Currently, only applies to VB class library projects
                return;
            }

            if (await project.NuGetReferences.IsTransitivelyAvailableAsync(SystemConfigurationPackageName, token).ConfigureAwait(false))
            {
                _logger.LogDebug("{PackageName} already referenced transitively", SystemConfigurationPackageName);
                return;
            }

            if (!state.Packages.Any(r => SystemConfigurationPackageName.Equals(r.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var systemConfigurationPackage = await _packageLoader.GetLatestVersionAsync(SystemConfigurationPackageName, state.TargetFrameworks, false, token).ConfigureAwait(false);

                if (systemConfigurationPackage is not null)
                {
                    _logger.LogInformation("Reference to configuration package ({SystemConfigurationPackageName}, version {SystemConfigurationPackageVersion}) needs added", SystemConfigurationPackageName, systemConfigurationPackage.Version);
                    state.Packages.Add(systemConfigurationPackage);
                }
                else
                {
                    _logger.LogWarning($"{SystemConfigurationPackageName} NuGet package reference cannot be added because the package cannot be found");
                }
            }
            else
            {
                _logger.LogDebug("Reference to configuration package ({SystemConfigurationPackageName}) already exists", SystemConfigurationPackageName);
            }
        }

        private static bool IsVbClassLibrary(IProject project)
        {
            return project.Language == Language.VisualBasic && project.OutputType == ProjectOutputType.Library;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    /// <summary>
    /// This analyzer will modify the vbproj of class libraries to resolve compilation errors:
    /// 1. Adding System.Configuration.ConfigurationManager resolves the error from Settings.Designer.vb
    /// 2. Adding <VBRuntime>Embed</VBRuntime> resolves the error from the vb compiler
    ///     Per https://github.com/dotnet/runtime/issues/30478#issuecomment-521270193.
    /// </summary>
    public class VbClassLibraryMyDotAnalyzer : IDependencyAnalyzer
    {
        private const string SystemConfigurationPackageName = "System.Configuration.ConfigurationManager";

        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<VbClassLibraryMyDotAnalyzer> _logger;

        public string Name => nameof(VbClassLibraryMyDotAnalyzer) + " reference analyzer";

        public VbClassLibraryMyDotAnalyzer(IPackageLoader packageLoader, ILogger<VbClassLibraryMyDotAnalyzer> logger, ITargetFrameworkMonikerComparer tfmComparer)
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

            if (!project.TargetFrameworks.Any(tfm => tfm.IsNetCore))
            {
                _logger.LogDebug("None of the tfms match packages from {PackageName}", SystemConfigurationPackageName);
                return;
            }

            if (!IsVbClassLibrary(project))
            {
                _logger.LogDebug("{Project} is not a VB class library", project.FileInfo);
                return;
            }

            if (string.IsNullOrWhiteSpace(project.GetFile().GetPropertyValue("VBRuntime")))
            {
                // resolves error BC30002: Type 'Global.Microsoft.VisualBasic.MyServices.Internal.ContextValue' is not defined.
                project.GetFile().SetPropertyValue("VBRuntime", "Embed");
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
                    // resolves error BC30002: Type 'Global.System.Configuration.ApplicationSettingsBase' is not defined.
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

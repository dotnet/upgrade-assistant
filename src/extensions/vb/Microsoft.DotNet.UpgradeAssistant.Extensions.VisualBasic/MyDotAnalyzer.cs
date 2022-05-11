// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic
{
    /// <summary>
    /// This analyzer will modify the vbproj of class libraries to resolve compilation errors.
    /// Adding System.Configuration.ConfigurationManager resolves compile errors from Settings.Designer.vb.
    /// </summary>
    public class MyDotAnalyzer : IDependencyAnalyzer
    {
        private const string SystemConfigurationPackageName = "System.Configuration.ConfigurationManager";

        private readonly ITransitiveDependencyIdentifier _transitiveIdentifier;
        private readonly IPackageLoader _packageLoader;
        private readonly ILogger<MyDotAnalyzer> _logger;

        public string Name => nameof(MyDotAnalyzer) + " reference analyzer";

        public MyDotAnalyzer(ITransitiveDependencyIdentifier transitiveIdentifier, IPackageLoader packageLoader, ILogger<MyDotAnalyzer> logger)
        {
            _transitiveIdentifier = transitiveIdentifier ?? throw new ArgumentNullException(nameof(transitiveIdentifier));
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

            if (!project.IsVbClassLibrary())
            {
                _logger.LogDebug("{Project} is not a VB class library", project.FileInfo);
                return;
            }

            if (await _transitiveIdentifier.IsTransitiveDependencyAsync(SystemConfigurationPackageName, project.NuGetReferences.PackageReferences, project.TargetFrameworks, token).ConfigureAwait(false))
            {
                _logger.LogDebug("{PackageName} already referenced transitively", SystemConfigurationPackageName);
                return;
            }

            if (!state.Packages.Any(r => SystemConfigurationPackageName.Equals(r.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var systemConfigurationPackage = await _packageLoader.GetLatestVersionAsync(SystemConfigurationPackageName, state.TargetFrameworks, new(), token).ConfigureAwait(false);

                if (systemConfigurationPackage is not null)
                {
                    // Note: Not expecting to see this package added explicitly. This resolves the error but the package is also included in the WindowCompatabilityPack which would make this reference transitively available.
                    // 1>C:\{ProjectDir}\My Project\Settings.Designer.vb(21,18): error BC30002: Type 'Global.System.Configuration.ApplicationSettingsBase' is not defined.
                    // 1>C:\{ProjectDir}\My Project\Settings.Designer.vb(57,10): error BC30002: Type 'Global.System.Configuration.UserScopedSettingAttribute' is not defined.
                    // 1>C:\{ProjectDir}\My Project\Settings.Designer.vb(59,10): error BC30002: Type 'Global.System.Configuration.DefaultSettingValueAttribute' is not defined.
                    var logMessage = SR.Format("Reference to configuration package ({0}, version {1}) needs to be added in order to resolve compile errors from Settings.Designer.vb", SystemConfigurationPackageName, systemConfigurationPackage.Version);
                    _logger.LogInformation(logMessage);
                    state.Packages.Add(systemConfigurationPackage, new OperationDetails { Details = new[] { logMessage } });
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
    }
}

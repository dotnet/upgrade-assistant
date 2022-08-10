// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade.Utils;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.UWPtoWinAppSDKUpgrade
{
    [ApplicableComponents(ProjectComponents.WinUI)]
    internal class WinUIReferenceAnalyzer : IDependencyAnalyzer
    {
        public string Name => "Windows App SDK package analysis";

        private const string CsWinRTPackageName = "Microsoft.Windows.CsWinRT";
        private const string CsWinRTVersion = "1.6.4";

        private readonly IPackageLoader _packageLoader;
        private readonly ILogger _logger;

        public WinUIReferenceAnalyzer(IPackageLoader packageLoader, ILogger<WinUIReferenceAnalyzer> logger)
        {
            this._packageLoader = packageLoader;
            this._logger = logger;
        }

        public async Task AnalyzeAsync(IProject project, IDependencyAnalysisState state, CancellationToken token)
        {
            if (!(await project.IsWinUIProjectAsync(token).ConfigureAwait(false)))
            {
                return;
            }

            if (project.AllProjectReferences().Any(id => id.Contains(".vcxproj")))
            {
                var newPackage = new NuGetReference(CsWinRTPackageName, CsWinRTVersion);
                state.Packages.Add(newPackage,
                    new OperationDetails() { Risk = BuildBreakRisk.Medium, Details = ImmutableList.Create<string>(newPackage.Name) });
            }

            foreach (var package in state.Packages)
            {
                if (package.Name.StartsWith("Microsoft.Toolkit", StringComparison.Ordinal))
                {
                    var newPackageName = package.Name == "Microsoft.Toolkit" ? "CommunityToolkit.Common"
                        : package.Name.Replace("Microsoft.Toolkit.Uwp", "CommunityToolkit.WinUI")
                        .Replace("Microsoft.Toolkit", "CommunityToolkit");

                    var newPackage = new NuGetReference(newPackageName, package.Version);
                    if (!await _packageLoader.DoesPackageSupportTargetFrameworksAsync(newPackage, project.TargetFrameworks, token).ConfigureAwait(true))
                    {
                        newPackage = await _packageLoader.GetLatestVersionAsync(newPackage.Name, project.TargetFrameworks,
                            new PackageSearchOptions { LatestMinorAndBuildOnly = false, Prerelease = false, Unlisted = false }, token).ConfigureAwait(true);
                        if (newPackage == null)
                        {
                            _logger.LogWarning($"Unable to find a supported WinUI nuget package for {package.Name}. Skipping this package.");
                            continue;
                        }
                    }

                    _logger.LogInformation($"UWP Package not supported. Replacing {package.Name} v{package.Version} with {newPackage.Name} v{newPackage.Version}");
                    state.Packages.Add(newPackage, new OperationDetails() { Risk = BuildBreakRisk.Medium, Details = ImmutableList.Create<string>(newPackage.Name) });
                    state.Packages.Remove(package, new OperationDetails() { Risk = BuildBreakRisk.Medium, Details = ImmutableList.Create<string>(package.Name) });
                }
            }
        }
    }
}

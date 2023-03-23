// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public class WindowsSdkTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        private readonly ILogger<WindowsSdkTargetFrameworkSelectorFilter> _logger;

        public WindowsSdkTargetFrameworkSelectorFilter(ILogger<WindowsSdkTargetFrameworkSelectorFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            if (TryGetMoniker(tfm, out var result))
            {
                if (tfm.TryUpdate(result))
                {
                    _logger.LogInformation("Recommending Windows TFM {TFM} for project {Name} because the project either has Windows-specific dependencies or builds to a WinExe", result, tfm.Project);
                }
            }
        }

        private static bool TryGetMoniker(ITargetFrameworkSelectorFilterState updater, [MaybeNullWhen(false)] out TargetFrameworkMoniker tfm)
        {
            var current = updater.Current;

            if (current.IsNetStandard)
            {
                current = updater.AppBase;
            }

            // Projects with Windows Desktop components or a WinExe output type should use a -windows suffix
            if (updater.Components.HasFlag(ProjectComponents.WindowsDesktop) || updater.Project.OutputType == ProjectOutputType.WinExe)
            {
                tfm = current with { Platform = TargetFrameworkMoniker.Platforms.Windows };

                if (updater.Components.HasFlag(ProjectComponents.WinRT))
                {
                    if (updater.Components.HasFlag(ProjectComponents.WinUI))
                    {
                        var existingTfm = updater.Project.TargetFrameworks.Count == 0 ? null : updater.Project.TargetFrameworks.First();
                        if (existingTfm == null
                            || existingTfm.PlatformVersion == null
                            || existingTfm.PlatformVersion.CompareTo(TargetFrameworkMoniker.Net50_Windows_10_0_19041_0.PlatformVersion) < 0)
                        {
                            // TODO: Default to this version to ensure everything is supported.
                            tfm = tfm with { PlatformVersion = TargetFrameworkMoniker.Net50_Windows_10_0_19041_0.PlatformVersion };
                        }
                        else
                        {
                            tfm = tfm with { PlatformVersion = existingTfm.PlatformVersion };
                        }
                    }
                    else
                    {
                        // TODO: Default to this version to ensure everything is supported.
                        tfm = tfm with { PlatformVersion = TargetFrameworkMoniker.Net50_Windows_10_0_19041_0.PlatformVersion };
                    }
                }

                return true;
            }

            tfm = null;
            return false;
        }
    }
}

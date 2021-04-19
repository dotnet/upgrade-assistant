// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.TargetFramework
{
    public class WindowsSdkTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            if (tfm is null)
            {
                throw new System.ArgumentNullException(nameof(tfm));
            }

            if (TryGetMoniker(tfm, out var result))
            {
                tfm.TryUpdate(result);
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
                    // TODO: Default to this version to ensure everything is supported.
                    tfm = tfm with { PlatformVersion = new Version(10, 0, 19041, 0) };
                }

                return true;
            }

            tfm = null;
            return false;
        }
    }
}

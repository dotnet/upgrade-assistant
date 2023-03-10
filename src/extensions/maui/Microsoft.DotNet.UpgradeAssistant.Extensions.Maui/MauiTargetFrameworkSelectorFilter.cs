// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Maui
{
    public class MauiTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        private readonly ILogger<MauiTargetFrameworkSelectorFilter> _logger;

        public MauiTargetFrameworkSelectorFilter(ILogger<MauiTargetFrameworkSelectorFilter> logger)
        {
            _logger = logger;
        }

        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            if (tfm.Components.HasFlag(ProjectComponents.XamarinAndroid))
            {
                _logger.LogInformation("Recommending TFM {TFM} for project {Name} because project is of type Xamarin.Android", TargetFrameworkMoniker.Net70_Android, tfm.Project);
                tfm.TryUpdate(TargetFrameworkMoniker.Net70_Android);
            }

            if (tfm.Components.HasFlag(ProjectComponents.XamariniOS))
            {
                _logger.LogInformation("Recommending TFM {TFM} for project {Name} because project is of type Xamarin.iOS", TargetFrameworkMoniker.Net70_iOS, tfm.Project);
                tfm.TryUpdate(TargetFrameworkMoniker.Net70_iOS);
            }

            if (tfm.Components.HasFlag(ProjectComponents.Maui) && tfm.Project?.TargetFrameworks.Count > 1)
            {
                TargetFrameworkMoniker targetFrameworkMoniker = tfm.Project.TargetFrameworks.FirstOrDefault();
                _logger.LogInformation("Recommending TFM {TFM} for project {Name} because project is of type .NET MAUI with multiple TFMs: {TFMs}", targetFrameworkMoniker, tfm.Project, string.Join(", ", tfm.Project.TargetFrameworks));
                tfm.TryUpdate(targetFrameworkMoniker);
            }
            else if (tfm.Components.HasFlag(ProjectComponents.MauiAndroid))
            {
                _logger.LogInformation("Recommending TFM {TFM} for project {Name} because project is of type .NET MAUI Target:Android", TargetFrameworkMoniker.Net70_Android, tfm.Project);
                tfm.TryUpdate(TargetFrameworkMoniker.Net70_Android);
            }
            else if (tfm.Components.HasFlag(ProjectComponents.MauiiOS))
            {
                _logger.LogInformation("Recommending TFM {TFM} for project {Name} because project is of type .NET MAUI Target:iOS", TargetFrameworkMoniker.Net70_iOS, tfm.Project);
                tfm.TryUpdate(TargetFrameworkMoniker.Net70_iOS);
            }
            else if (tfm.Components.HasFlag(ProjectComponents.Maui))
            {
                _logger.LogInformation("Recommending TFM {TFM} for project {Name} because project is of type .NET MAUI", TargetFrameworkMoniker.Net70_Android, tfm.Project);
                tfm.TryUpdate(TargetFrameworkMoniker.Net70_Android);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
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
                _logger.LogInformation("Project {Name} is of type Xamarin.Android, migration to .NET MAUI requires to be least net6.0-android.", tfm.Project);
                tfm.TryUpdate(TargetFrameworkMoniker.Net60_Android);
            }

            if (tfm.Components.HasFlag(ProjectComponents.XamariniOS))
            {
                _logger.LogInformation("Project {Name} is of type Xamarin.iOS, migration to .NET MAUI requires to be least net6.0-ios.", tfm.Project);
                tfm.TryUpdate(TargetFrameworkMoniker.Net60_iOS);
            }
        }
    }
}

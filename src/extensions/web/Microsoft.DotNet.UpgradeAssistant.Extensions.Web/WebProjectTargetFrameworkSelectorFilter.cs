// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Web
{
    public class WebProjectTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        private readonly ILogger<WebProjectTargetFrameworkSelectorFilter> _logger;

        public WebProjectTargetFrameworkSelectorFilter(ILogger<WebProjectTargetFrameworkSelectorFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            if (tfm.Components.HasFlag(ProjectComponents.AspNet) || tfm.Components.HasFlag(ProjectComponents.AspNetCore))
            {
                if (tfm.TryUpdate(tfm.AppBase))
                {
                    _logger.LogInformation("Recommending executable TFM {TFM} for project {Name} because the project builds to a web app", tfm.AppBase, tfm.Project);
                }
            }
        }
    }
}

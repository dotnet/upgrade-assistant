// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.TargetFramework
{
    public class ExecutableTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        private readonly ILogger<ExecutableTargetFrameworkSelectorFilter> _logger;

        public ExecutableTargetFrameworkSelectorFilter(ILogger<ExecutableTargetFrameworkSelectorFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            if (tfm.Project.OutputType == ProjectOutputType.Exe)
            {
                if (tfm.TryUpdate(tfm.AppBase))
                {
                    _logger.LogInformation("Recommending executable TFM {TFM} for project {Name} because the project builds to an executable", tfm.AppBase, tfm.Project);
                }
            }
        }
    }
}

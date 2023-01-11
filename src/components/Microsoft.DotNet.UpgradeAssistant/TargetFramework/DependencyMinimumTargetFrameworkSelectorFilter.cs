// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.TargetFramework
{
    /// <summary>
    /// An implementation of <see cref="ITargetFrameworkSelectorFilter"/> that ensures that the target framework is at least
    /// as high as the minimum of each of a project's dependencies. If we did not check for this, then a project may not be
    /// able to reference its dependencies anymore.
    /// </summary>
    public class DependencyMinimumTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        private readonly ILogger<DependencyMinimumTargetFrameworkSelectorFilter> _logger;
        private readonly ITargetFrameworkMonikerComparer _tfmComparer;

        public DependencyMinimumTargetFrameworkSelectorFilter(ITargetFrameworkMonikerComparer comparer, ILogger<DependencyMinimumTargetFrameworkSelectorFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tfmComparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            if (tfm.Components.HasFlag(ProjectComponents.MauiAndroid) || tfm.Components.HasFlag(ProjectComponents.MauiiOS) || tfm.Components.HasFlag(ProjectComponents.Maui))
            {
                _logger.LogInformation("Skip minimum dependency check because .NET MAUI supports multiple TFMs.");
            }
            else if (tfm.Components.HasFlag(ProjectComponents.WinUI))
            {
                _logger.LogInformation("Skip minimum dependency check because Windows App SDK cannot work with targets lower than already recommended TFM.");
            }
            else
            {
                foreach (var dep in tfm.Project.ProjectReferences)
                {
                    var min = dep.TargetFrameworks.OrderBy(t => t, _tfmComparer)
                        .Where(tfm => !tfm.IsFramework)
                        .FirstOrDefault();

                    if (min is not null)
                    {
                        if (tfm.TryUpdate(min))
                        {
                            _logger.LogInformation("Recommending TFM {TFM} for project {Name} because of dependency on project {Dependency}", min, tfm.Project, dep.GetFile().FilePath);
                        }
                    }
                }
            }
        }
    }
}

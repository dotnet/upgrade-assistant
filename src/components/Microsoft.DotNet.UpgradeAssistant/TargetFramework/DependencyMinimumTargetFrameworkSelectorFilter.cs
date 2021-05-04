// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.TargetFramework
{
    /// <summary>
    /// An implementation of <see cref="ITargetFrameworkSelectorFilter"/> that ensures that the target framework is at least
    /// as high as the minimum of each of a project's dependencies. If we did not check for this, then a project may not be
    /// able to reference its dependencies anymore.
    /// </summary>
    public class DependencyMinimumTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        private readonly ITargetFrameworkMonikerComparer _tfmComparer;

        public DependencyMinimumTargetFrameworkSelectorFilter(ITargetFrameworkMonikerComparer comparer)
        {
            _tfmComparer = comparer;
        }

        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            foreach (var dep in tfm.Project.ProjectReferences)
            {
                var min = dep.TargetFrameworks.OrderBy(t => t, _tfmComparer)
                    .Where(tfm => !tfm.IsFramework)
                    .FirstOrDefault();

                if (min is not null)
                {
                    tfm.TryUpdate(min);
                }
            }
        }
    }
}

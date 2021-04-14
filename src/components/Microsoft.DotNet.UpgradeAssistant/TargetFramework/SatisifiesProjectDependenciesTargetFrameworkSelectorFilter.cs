// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.TargetFramework
{
    public class SatisifiesProjectDependenciesTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        private readonly ITargetFrameworkMonikerComparer _tfmComparer;

        public SatisifiesProjectDependenciesTargetFrameworkSelectorFilter(ITargetFrameworkMonikerComparer comparer)
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
                EnsureNoDowngrade(tfm, dep.TargetFrameworks);
            }
        }

        private void EnsureNoDowngrade(ITargetFrameworkSelectorFilterState tfm, IEnumerable<TargetFrameworkMoniker> others)
        {
            var min = others.OrderBy(t => t, _tfmComparer)
                .Where(tfm => !tfm.IsFramework)
                .FirstOrDefault();

            if (min is not null)
            {
                if (_tfmComparer.Compare(min, tfm.Current) > 0)
                {
                    tfm.TryUpdate(min);
                }
            }
        }
    }
}

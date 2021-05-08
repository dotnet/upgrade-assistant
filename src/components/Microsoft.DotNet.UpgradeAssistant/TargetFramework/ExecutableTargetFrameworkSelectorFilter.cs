// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.TargetFramework
{
    public class ExecutableTargetFrameworkSelectorFilter : ITargetFrameworkSelectorFilter
    {
        public void Process(ITargetFrameworkSelectorFilterState tfm)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            if (tfm.Project.OutputType == ProjectOutputType.Exe)
            {
                tfm.TryUpdate(tfm.AppBase);
            }
        }
    }
}
